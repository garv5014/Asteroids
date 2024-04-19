namespace Asteroids.Shared.GameEntities;

public class Asteroid(
  int xCoordinate,
  int yCoordinate,
  int rotation,
  int size,
  double velocityX,
  double velocityY
) : GameObject(xCoordinate, yCoordinate, rotation, size, velocityX, velocityY);