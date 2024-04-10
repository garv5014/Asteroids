namespace Asteroids.Shared.GameEntities;

public class Asteroid(int xCoordinate, int yCoordinate, int rotation, int size, double velocityX, double velocityY)
  : IGameObject
{
  public int XCoordinate { get; set; } = xCoordinate;
  public int YCoordinate { get; set; } = yCoordinate;
  public int Rotation { get; set; } = rotation;
  public int Size { get; set; } = size;
  public double VelocityX { get; set; } = velocityX;
  public double VelocityY { get; set; } = velocityY;
}