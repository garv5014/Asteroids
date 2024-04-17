namespace Asteroids.Shared.GameEntities;

public class GameObject(int xCoordinate, int yCoordinate, int rotation, int size, double velocityX, double velocityY)
{
    public Guid Id { get; } = Guid.NewGuid();
    public int XCoordinate { get; set; } = xCoordinate;
    public int YCoordinate { get; set; } = yCoordinate;
    public int Rotation { get; set; } = rotation;
    public int Size { get; set; } = size;
    public double VelocityX { get; set; } = velocityX;
    public double VelocityY { get; set; } = velocityY;

    public bool CheckCollision(GameObject otherObject)
    {
        var distance = Math.Abs(otherObject.XCoordinate - XCoordinate)
                       + Math.Abs(otherObject.YCoordinate - YCoordinate);
        return distance < ((otherObject.Size / 2) + (Size / 2));
    }
}