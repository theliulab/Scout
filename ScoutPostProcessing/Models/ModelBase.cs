namespace ScoutPostProcessing.Models
{
    public abstract class ModelBase
    {

        public abstract void Train(double[][] x, bool[] y);

        public abstract double[] Score(double[][] x);

        public double[] BoolToDoubleArray(bool b)
        {
            if (b == true)
                return new double[] { 1, 0 };
            else
                return new double[] { 0, 1 };
        }

        public float[] BoolToFloatArray(bool b)
        {
            if (b == true)
                return new float[] { 1f, 0f };
            else
                return new float[] { 0f, 1f };
        }
    }
}