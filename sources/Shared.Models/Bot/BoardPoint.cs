namespace Shared.Models.Bot
{
    public readonly record struct BoardPoint(double X, double Y)
    {
        public double Radius => Math.Sqrt(X * X + Y * Y);
    }
}
