using Microsoft.Extensions.Logging;

namespace Asteroids.Shared.GameEntities;

public class Game
{
    private int boardHeight { get; set; }
    private int boardWidth { get; set; }

    public List<((int xEdge, int yEdge), (int headingMin, int headingMax))> edges { get; set; } =
        new List<((int xEdge, int yEdge), (int headingMin, int headingMax))>
        {
            ((595, 0), (0, 180)), // top
            ((0, 595), (180, 360)), // left
            ((595, 595), (90, 270)), // bottom
            ((595, 0), (270, 430)) // right
        };
    private readonly Random _random = new();
    private Dictionary<string, Ship> ships { get; set; } = new();
    private List<Asteroid> asteroids { get; set; } = new();

    public Game(int boardHeight, int boardWidth)
    {
        this.boardHeight = boardHeight;
        this.boardWidth = boardWidth;
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

    public List<Ship> GetShips()
    {
        return ships.Values.ToList();
    }

    public List<Asteroid> GetAsteroids()
    {
        return asteroids;
    }

    public void UpdateShip(string key, UpdateShipParams value)
    {
        // Update ship movement in game state
        if (ships.TryGetValue(key, out var ship))
        {
            ship.ShipMovement = value;
        }
    }

    private void MoveShips()
    {
        foreach (var shipEntry in ships)
        {
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

    public void SpawnAsteroids()
    {
        var randomEdge = _random.Next(0, edges.Count);
        var edge = edges[randomEdge];
        var spawnX = _random.Next(edge.Item1.xEdge);
        var spawnY = randomEdge == 2 ? edge.Item1.yEdge : _random.Next(edge.Item1.yEdge);
        var size = _random.Next(20, 100);
        var heading = _random.Next(edge.Item2.headingMin, edge.Item2.headingMax);
        double headingRadians = Math.PI * heading / 180.0;
        double speedFactor = 4; // Adjust this value to control asteroid speed

        var asteroid = new Asteroid(
            xCoordinate: spawnX,
            yCoordinate: spawnY,
            rotation: heading,
            size: size,
            velocityX: Math.Cos(headingRadians) * speedFactor,
            velocityY: Math.Sin(headingRadians) * speedFactor
        );

        asteroids.Add(asteroid);
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

public static class GameExtensions
{
    public static LobbyState LobbyStateSnapShot(this Game game, LobbyStatus currentStatus)
    {
        var ships = game.GetShips();
        var lobbyState = new LobbyState(
            isOwner: false,
            playerCount: ships.Count,
            currentStatus: currentStatus,
            ships: ships,
            asteroids: game.GetAsteroids()
        );

        return lobbyState;
    }
}
