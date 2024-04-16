namespace Asteroids.Shared.GameEntities;

public class Ship(int xCoordinate, int yCoordinate, int rotation)
  : GameObject(xCoordinate, yCoordinate, rotation, 20, 0, 0)
{
  public UpdateShipParams ShipMovement { get; set; } = new(false, false, false, false);
  public int Health { get; set; } = 50;
  public int Score { get; set; }
}

public record UpdateShipParams(bool IsThrusting, bool IsRotatingRight, bool IsRotatingLeft, bool IsShooting);