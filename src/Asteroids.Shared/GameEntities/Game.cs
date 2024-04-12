using Akka.Util;
using Microsoft.Extensions.Logging;

namespace Asteroids.Shared.GameEntities;

public class Game
{
    private readonly ILogger<Game> _log;
    private int boardHeight { get; set; }
    private int boardWidth { get; set; }

    private Dictionary<string, Ship> ships { get; set; } = new();
    private List<Asteroid> asteroids { get; set; } = new();

    public Game(int boardHeight, int boardWidth, ILogger<Game> logger)
    {
        boardHeight = boardHeight;
        boardWidth = boardWidth;
        _log = logger;
    }

    public void AddShip(string key, Ship ship)
    {
        ships.Add(key, ship);
    }

    public void Tick()
    {
        MoveShips();
        MoveAsteroids();
        CheckCollisions();
    }

    private void MoveShips()
    {
        _log.LogInformation("Moving ships");
        foreach (var shipEntry in ships)
        {
            _log.LogInformation("Moving ship {0}", shipEntry.Key);
            var ship = shipEntry.Value;

            // Handle rotation
            if (ship.ShipMovement.IsRotatingRight)
            {
                ship.Rotation -= 5;
            }
            else if (ship.ShipMovement.IsRotatingLeft)
            {
                ship.Rotation += 5;
            }

            ship.Rotation = (ship.Rotation + 360) % 360;

            if (ship.ShipMovement.IsThrusting)
            {
                double radians = Math.PI * ship.Rotation / 180.0;
                ship.VelocityX += Math.Cos(radians) * 0.1;
                ship.VelocityY += Math.Sin(radians) * 0.1;
            }

            ship.XCoordinate += (int)ship.VelocityX;
            ship.YCoordinate += (int)ship.VelocityY;

            ship.XCoordinate = (ship.XCoordinate + boardWidth) % boardWidth;
            ship.YCoordinate = (ship.YCoordinate + boardHeight) % boardHeight;
        }
    }

    private void MoveAsteroids()
    {
        _log.LogInformation("Moving asteroids");

        var asteroidsToRemove = new List<Asteroid>();
        foreach (var asteroid in asteroids)
        {
            if (isOutOfBounds(asteroid))
            {
                asteroidsToRemove.Add(asteroid);
            }
            else
            {
                asteroid.XCoordinate += (int)asteroid.VelocityX;
                asteroid.YCoordinate += (int)asteroid.VelocityY;
            }
        }

        // Now remove the asteroids that need to be removed.
        foreach (var asteroid in asteroidsToRemove)
        {
            asteroids.Remove(asteroid);
        }
    }

    private bool isOutOfBounds(Asteroid asteroid)
    {
        return asteroid.YCoordinate > boardHeight
            || asteroid.YCoordinate < 0
            || asteroid.XCoordinate > boardWidth
            || asteroid.XCoordinate < 0;
    }

    private void CheckCollisions()
    {
        var asteroidsToRemove = new List<Asteroid>();
        foreach (var asteroid in asteroids)
        {
            foreach (var shipEntry in ships)
            {
                var ship = shipEntry.Value;

                if (ship.CheckCollisions(asteroid))
                {
                    ship.Health -= 10;
                    asteroidsToRemove.Add(asteroid);

                    if (ship.Health <= 0)
                    {
                        // Ship destroyed
                        ships.Remove(shipEntry.Key);
                    }
                }
            }
        }

        // Now remove the asteroids separately to avoid concurrent changes to the list.
        foreach (var asteroid in asteroidsToRemove)
        {
            asteroids.Remove(asteroid);
        }
    }
}
