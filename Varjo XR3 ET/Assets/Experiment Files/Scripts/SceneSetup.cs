using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Setup
{
    public enum Layout
    {
        BOX,
        FRUSTRUM
    }

    public enum TD_Form
    {
        SINGLE_SHAPE,
        MULTI_SHAPE,
        LETTERS
    }

    public enum Distribution
    {
        ROWS_RAND,
        PLANES_2,
        PLANES_4,
        RANDOM,
        GRID_JITTER,
        LOAD
    }

    public enum Variances
    { 
        NONE,
        SIZE
    }


    public class SceneSetup : MonoBehaviour
    {
        public bool live=false;

        [Tooltip("Set Seed to -1 for random seed")]
        public int seed = -1;
        public static int Seed;

        [Header("Layout Specifics")]
        public Layout layout;
        public static Layout Layout;
        public Distribution distribution;
        public static Distribution Distribution;

        [DrawIf("layout", Layout.BOX)]
        public Vector3 dimensions = new Vector3();
        public static Vector3 Dimensions;
        [DrawIf("layout", Layout.BOX)]
        public Vector3 offSet = new Vector3();
        public static Vector3 Offset;

        [DrawIf("layout", Layout.FRUSTRUM)]
        public Vector2 angles = new Vector2();
        public static Vector2 Angles;
        [DrawIf("layout", Layout.FRUSTRUM)]
        public Vector2 limits = new Vector2();
        public static Vector2 Limits;

        [DrawIf("distribution",Distribution.ROWS_RAND)] 
        public int nRows;
        public static int NRows;

        [Header("Target Specifics")]
        public TD_Form targetType;
        public static TD_Form TargetType;
        public Variances variance;
        public static Variances Variance;
        public int targetCount;
        public static int TargetCount;
        public int distractorCount;
        public static int DistractorCount;
        public float targetSize;
        public static float TargetSize;

        public Color[] colours;
        public static Color[] Colours;

        void Awake()
        {
            Layout = layout; Dimensions = dimensions; Angles = angles; Limits = limits;
            Distribution = distribution; Offset = offSet; Variance = variance; NRows = nRows;
            TargetType = targetType; TargetCount = targetCount; DistractorCount = distractorCount;
            Colours = colours; TargetSize = targetSize;
            if (seed != -1) Random.InitState(seed);
            print("Setup Completed");
            //Better way to do this, just can't remember term
            if (DistractorCount % (Colours.Length-1) != 0) Debug.LogWarning("Not an equal number of Distractors of each Colour");
        }

        void Update()
        {
            if (live)
            {
                Layout = layout; Dimensions = dimensions; Angles = angles; Limits = limits;
                Distribution = distribution; Offset = offSet; Variance = variance; NRows = nRows;
                TargetType = targetType; TargetCount = targetCount; DistractorCount = distractorCount;
                Colours = colours; TargetSize = targetSize;
                if (seed != -1) Random.InitState(seed);
                print("Setup Completed");
                //Better way to do this, just can't remember term
                if (DistractorCount % (Colours.Length - 1) != 0) Debug.LogWarning("Not an equal number of Distractors of each Colour");
            }
        }
    }
}
