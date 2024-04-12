namespace Asteroids.Shared.GameEntities;

public class IGameObject
{
    public int XCoordinate { get; set; }
    public int YCoordinate { get; set; }
    public int Rotation { get; set; }

    public int Size { get; set; }

    public bool CheckCollisions(IGameObject other)
    {
        double distance = Math.Sqrt(
            Math.Pow(other.XCoordinate - this.XCoordinate, 2)
                + Math.Pow(other.YCoordinate - this.YCoordinate, 2)
        );

        return distance < (other.Size / 2) + (this.Size / 2);
    }
}
