namespace Asteroids.Shared.GameEntities;

public class Ship(int id, int xCoordinate, int yCoordinate, int rotation)
  : IGameObject
{
  public int Id { get; set; } = id;
  public int XCoordinate { get; set; } = xCoordinate;
  public int YCoordinate { get; set; } = yCoordinate;
  public int Rotation { get; set; } = rotation;
  public UpdateShipParams UpdateShipParams { get; set; } = new UpdateShipParams(false, false, false);
}

public record UpdateShipParams(bool IsThrusting, bool IsRotatingRight, bool IsRotatingLeft);