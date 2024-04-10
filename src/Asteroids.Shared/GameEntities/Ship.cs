namespace Asteroids.Shared.GameEntities;

public class Ship(int xCoordinate, int yCoordinate, int rotation) : IGameObject
{
    public int XCoordinate { get; set; } = xCoordinate;
    public int YCoordinate { get; set; } = yCoordinate;
    public int Rotation { get; set; } = rotation;
    public UpdateShipParams ShipMovement { get; set; } = new UpdateShipParams(false, false, false);

    // New properties for velocity
    public double VelocityX { get; set; } = 0;
    public double VelocityY { get; set; } = 0;
}

public record UpdateShipParams(bool IsThrusting, bool IsRotatingRight, bool IsRotatingLeft);
