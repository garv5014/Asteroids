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

    public int Health { get; set; } = 50; // Default health
    public int Size { get; set; } = 20;

    public bool CheckCollisionAsteroid(Asteroid other)
    {
        double distance = Math.Sqrt(
            Math.Pow(other.XCoordinate - this.XCoordinate, 2)
                + Math.Pow(other.YCoordinate - this.YCoordinate, 2)
        );
        Console.WriteLine(
            $"Distance: {distance} {this.XCoordinate} {this.YCoordinate} {other.XCoordinate} {other.YCoordinate} {this.Size} {other.Size}"
        );
        return distance < ((other.Size / 2) + (this.Size / 2));
    }
}

public record UpdateShipParams(bool IsThrusting, bool IsRotatingRight, bool IsRotatingLeft);
