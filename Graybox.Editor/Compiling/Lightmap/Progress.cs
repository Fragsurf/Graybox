namespace Graybox.Editor.Compiling.Lightmap
{
    public struct Progress
    {
        public readonly string Message;
        public readonly float Amount;

        public Progress(string msg, float amount)
        {
            Message = msg; Amount = amount;
        }
    }
}
