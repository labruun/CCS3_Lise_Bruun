# import libraries
library(stringi)

# START METHODS ------------------------------------------------
# calculate median
mdn_sd <- function(vals){
  median <- median(vals)
  sumOfMedians <- 0
  
  for (i in 1:length(vals)){
    distance <- (vals[i] - median)^2
    sumOfMedians <- sumOfMedians + distance
  }
  
  return(sqrt(sumOfMedians/(length(vals) - 1)))
}

# plot results
diagnostic.plot <- function(samples, fixations, start.event=1, start.time=NULL, duration=1000, ...) {
  
  stopifnot(start.event >= 1)
  stopifnot(start.event <= nrow(fixations))
  stopifnot(start.time  >= min(samples$frame))
  stopifnot(start.time  <= max(samples$frame))
  
  if (is.null(start.time))
    start.time <- fixations$start[start.event]
  
  graphics::par(mar=c(2,2,0,0))
  if ("ylim" %in% list(...))
    with(samples,   plot(frame, x, pch=20, cex=0.3, col="red",
                         ylim=c(min(fixations$x, fixations$y),
                                max(fixations$x, fixations$y)),
                         xlim=c(start.time, start.time+duration)))
  else
    with(samples,   plot(frame, x, pch=20, cex=0.3, col="red",
                         xlim=c(start.time, start.time+duration), ...))
  with(samples,   points(frame, y, pch=20, cex=0.3, col="orange"))
  with(fixations, lines(zip(start, end, NA), rep(x, each=3)))
  with(fixations, lines(zip(start, end, NA), rep(y, each=3)))
  with(fixations, abline(v=c(start, end), col="lightgrey"))
  
}

# Pairs plot using fixation x- and y-dispersion and duration and color
# for detected event type (black=fixation, red=blink, blue=too short,
# green=too dispersed).
diagnostic.plot.event.types <- function(fixations) {
  graphics::pairs( ~ log10(mad.x+0.001) + log10(mad.y+0.001) + log10(dur),
                   data=fixations,
                   col=fixations$event)
}

detect.fixations <- function(samples, lambda=6, smooth.coordinates=FALSE, smooth.saccades=TRUE) {
  
  if (smooth.coordinates) {
    # Keep and reuse original first and last coordinates as they can't
    # be smoothed:
    x <- samples$x[c(1,nrow(samples))]
    y <- samples$y[c(1,nrow(samples))]
    kernel <- rep(1/3, 3)
    samples$x <- stats::filter(samples$x, kernel)
    samples$y <- stats::filter(samples$y, kernel)
    # Plug in the original values:
    samples$x[c(1,nrow(samples))] <- x
    samples$y[c(1,nrow(samples))] <- y
  }
  
  samples <- detect.saccades(samples, lambda, smooth.saccades)
  
  if (all(!samples$saccade))
    stop("No saccades were detected.  Something went wrong.")
  
  fixations <- aggregate.fixations(samples)
  
  fixations$event <- label.blinks.artifacts(fixations)
  
  fixations
  
}

aggregate.fixations <- function(samples) {
  
  # In saccade.events a 1 marks the start of a saccade and a -1 the
  # start of a fixation.
  saccade.events <- sign(c(0, diff(samples$saccade)))
  
  # New fixations start either when a saccade ends
  samples$fixation.id <- cumsum(saccade.events==-1)
  samples$t2 <- samples$frame
  samples$t2 <- samples$t2[2:(nrow(samples)+1)]
  # Set last t2 value in a trial to last time value to avoid -Inf dur
  # and end values when the last event has just one sample (see #13 on
  # Github).  May produce zero-duration events but zero simply is our
  # most conservative guess in this case.
  samples$t2 <- with(samples, ifelse(is.na(t2), frame, t2))
  
  # Discard samples that occurred during saccades:
  samples <- samples[!samples$saccade,,drop=FALSE]
  
  fixations <- with(samples, data.frame(
    start   = tapply(frame,  fixation.id, min),
    end     = tapply(t2,    fixation.id, function(x) max(x, na.rm=TRUE)),
    x       = tapply(x,     fixation.id, stats::median),
    y       = tapply(y,     fixation.id, stats::median),
    mad.x   = tapply(x,     fixation.id, stats::mad),
    mad.y   = tapply(y,     fixation.id, stats::mad),
    peak.vx = tapply(vx,    fixation.id, function(x) x[which.max(abs(x))]),
    peak.vy = tapply(vy,    fixation.id, function(x) x[which.max(abs(x))]),
    stringsAsFactors=FALSE))
  
  fixations$dur <- fixations$end - fixations$start
  
  fixations
  
}

# label artifacst
label.blinks.artifacts <- function(fixations) {
  
  # Blink and artifact detection based on dispersion:
  lsdx <- log10(fixations$mad.x)
  lsdy <- log10(fixations$mad.y)
  median.lsdx <- stats::median(lsdx, na.rm=TRUE)
  median.lsdy <- stats::median(lsdy, na.rm=TRUE)
  mad.lsdx <- stats::mad(lsdx, na.rm=TRUE)
  mad.lsdy <- stats::mad(lsdy, na.rm=TRUE)
  
  # Dispersion too low -> blink:
  threshold.lsdx <- median.lsdx - 4 * mad.lsdx
  threshold.lsdy <- median.lsdy - 4 * mad.lsdy
  event <- ifelse((!is.na(lsdx) & lsdx < threshold.lsdx) &
                    (!is.na(lsdy) & lsdy < threshold.lsdy),
                  "blink", "fixation")
  
  # Dispersion too high -> artifact:
  threshold.lsdx <- median.lsdx + 4 * mad.lsdx
  threshold.lsdy <- median.lsdy + 4 * mad.lsdy
  event <- ifelse((!is.na(lsdx) & lsdx > threshold.lsdx) &
                    (!is.na(lsdy) & lsdy > threshold.lsdy),
                  "too dispersed", event)
  
  # Artifact detection based on duration:
  dur <- 1/fixations$dur
  median.dur <- stats::median(dur, na.rm=TRUE)
  mad.dur <- stats::mad(dur, na.rm=TRUE)
  
  # Duration too short -> artifact:
  threshold.dur <- median.dur + mad.dur * 5
  event <- ifelse(event!="blink" & dur > threshold.dur, "too short", event)
  
  factor(event, levels=c("fixation", "blink", "too dispersed", "too short"))
}

# detect saccades
detect.saccades <- function(samples, lambda, smooth.saccades) {
  
  # Calculate horizontal and vertical velocities:
  vx <- stats::filter(samples$x, -1:1/2)
  vy <- stats::filter(samples$y, -1:1/2)
  
  # We don't want NAs, as they make our life difficult later
  # on.  Therefore, fill in missing values:
  vx[1] <- vx[2]
  vy[1] <- vy[2]
  vx[length(vx)] <- vx[length(vx)-1]
  vy[length(vy)] <- vy[length(vy)-1]
  
  msdx <- sqrt(stats::median(vx**2, na.rm=TRUE) - stats::median(vx, na.rm=TRUE)**2)
  msdy <- sqrt(stats::median(vy**2, na.rm=TRUE) - stats::median(vy, na.rm=TRUE)**2)
  
  radiusx <- msdx * lambda
  radiusy <- msdy * lambda
  
  sacc <- ((vx/radiusx)**2 + (vy/radiusy)**2) > 1
  if (smooth.saccades) {
    sacc <- stats::filter(sacc, rep(1/3, 3))
    sacc <- as.logical(round(sacc))
  }
  samples$saccade <- ifelse(is.na(sacc), FALSE, sacc)
  samples$vx <- vx
  samples$vy <- vy
  
  samples
  
}
# END METHODS ------------------------------------------------

# START PROGRAM ----------------------------------------------

# load dataset
setwd('/Users/tmq470/Documents/GitHub/SubtleGazeDirectionVR/Analysis')
data <- read.csv('data/example_data.csv', sep=';')

# get interesting rows
data <- data [4500:5500, ]

# preapre data for fixation detection
samples <- data.frame(matrix(nrow=nrow(data), ncol=3))
colnames(samples) <- c("x", "y", "frame")
samples$x <- data$CombinedGazeForward.x
samples$y <- data$CombinedGazeForward.y
samples$frame <- data$Frame

# detect fixations and saccades
detected_fixations <- detect.fixations(samples, lambda = 6, smooth.coordinates = FALSE, smooth.saccades = TRUE)

# plot result
diagnostic.plot(samples, detected_fixations, start.time = samples$frame[1], duration=1000, ylim=c(0,1))




