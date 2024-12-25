
library("devtools")
install_github("tmalsburg/saccades/saccades", dependencies=TRUE)
library(zoom)
library(saccades)

setwd('/Users/tmq470/Documents/GitHub/SubtleGazeDirectionVR/Analysis')
my_samples <- read.csv('data/example_data.csv', sep=';')

# get interesting rows
my_samples <- my_samples [4500:5500, ]
trial <- data.frame(matrix(1, nrow = nrow(my_samples), ncol = 1))
test_samples <- cbind(my_samples$Frame, my_samples$CombinedGazeForward.x, my_samples$CombinedGazeForward.y, trial)
colnames(test_samples) <- c("time", "x", "y", "trial")

fixations <- saccades::detect.fixations(test_samples)
diagnostic.plot(test_samples, fixations, start.time=test_samples[1,1], duration=1000, interactive=FALSE, ylim=c(0,1))


