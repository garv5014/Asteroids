namespace Asteroids.Shared.GameEntities;

public class Game(int boardHeight, int boardWidth)
{
    public int BoardHeight { get; set; } = boardHeight;
    public int BoardWidth { get; set; } = boardWidth;

    private List<((int xEdge, int yEdge), (int headingMin, int headingMax))> Edges { get; set; } =
        [
            ((600, 0), (0, 180)), // top
            ((0, 600), (180, 360)), // left
            ((600, 600), (90, 270)), // bottom
            ((600, 0), (270, 430))
        ];

    private readonly Random _random = new();
    private Dictionary<string, Ship> Ships { get; set; } = new();
    private List<Asteroid> Asteroids { get; set; } = new();
    private List<Projectile> Projectiles { get; set; } = new();

    public void AddShip(string key, Ship ship)
    {
        Ships.Add(key, ship);
    }

    public void Tick()
    {
        MoveShips();
        MoveAsteroids();
        MoveProjectiles();
        CheckCollisions();
    }

    public List<Ship> GetShips()
    {
        return Ships.Values.ToList();
    }

    public List<Asteroid> GetAsteroids()
    {
        return Asteroids;
    }

    public List<Projectile> GetProjectiles()
    {
        return Projectiles;
    }

    public void UpdateShip(string key, UpdateShipParams value)
    {
        // Update ship movement in game state
        if (Ships.TryGetValue(key, out var ship))
        {
            ship.ShipMovement = value;
        }
    }

    private void MoveShips()
    {
        foreach (var shipEntry in Ships)
        {
            var ship = shipEntry.Value;

            // Handle rotation
            if (ship.ShipMovement.IsRotatingRight)
            {
                ship.Rotation -= 10;
            }
            else if (ship.ShipMovement.IsRotatingLeft)
            {
                ship.Rotation += 10;
            }

            var radians = Math.PI * ship.Rotation / 180.0;
            if (ship.ShipMovement.IsThrusting)
            {
                ship.VelocityX += Math.Cos(radians);
                ship.VelocityY += Math.Sin(radians);
            }
            else
            {
                approachZeroVelocity(ship);
            }

            if (ship.ShipMovement.IsShooting)
            {
                var newProjectile = new Projectile(
                    xCoordinate: ship.XCoordinate,
                    yCoordinate: ship.YCoordinate,
                    rotation: ship.Rotation,
                    size: 20,
                    velocityX: ship.VelocityX + Math.Cos(Math.PI * ship.Rotation / 180.0) * 10,
                    velocityY: ship.VelocityY + Math.Sin(Math.PI * ship.Rotation / 180.0) * 10
                );
                Projectiles.Add(newProjectile);
            }

            ship.XCoordinate += (int)ship.VelocityX;
            ship.YCoordinate += (int)ship.VelocityY;

            ship.XCoordinate = (ship.XCoordinate + BoardWidth) % BoardWidth;
            ship.YCoordinate = (ship.YCoordinate + BoardHeight) % BoardHeight;
        }
    }

    private void approachZeroVelocity(Ship ship)
    {
        double slowDownFactor = 0.25;
        if (ship.VelocityX > 0)
        {
            ship.VelocityX -= slowDownFactor;
        }
        else if (ship.VelocityX < 0)
        {
            ship.VelocityX += slowDownFactor;
        }

        if (ship.VelocityY > 0)
        {
            ship.VelocityY -= slowDownFactor;
        }
        else if (ship.VelocityY < 0)
        {
            ship.VelocityY += slowDownFactor;
        }
    }

    private void MoveAsteroids()
    {
        var asteroidsToRemove = new List<Asteroid>();
        foreach (var asteroid in Asteroids)
        {
            if (IsOutOfBounds(asteroid))
            {
                asteroidsToRemove.Add(asteroid);
            }
            else
            {
                asteroid.Rotation += 6;
                asteroid.XCoordinate += (int)asteroid.VelocityX;
                asteroid.YCoordinate += (int)asteroid.VelocityY;
            }
        }

        // Now remove the asteroids that need to be removed.
        foreach (var asteroid in asteroidsToRemove)
        {
            Asteroids.Remove(asteroid);
        }
    }

    private void MoveProjectiles()
    {
        var projectilesToRemove = new List<Projectile>();
        foreach (var projectile in Projectiles)
        {
            if (IsOutOfBounds(projectile))
            {
                projectilesToRemove.Add(projectile);
            }
            else
            {
                projectile.XCoordinate += (int)projectile.VelocityX;
                projectile.YCoordinate += (int)projectile.VelocityY;
            }
        }

        // Now remove the projectiles that need to be removed.
        foreach (var projectile in projectilesToRemove)
        {
            Projectiles.Remove(projectile);
        }
    }

    private bool IsOutOfBounds(GameObject gameObject)
    {
        return gameObject.YCoordinate > BoardHeight
            || gameObject.YCoordinate < 0
            || gameObject.XCoordinate > BoardWidth
            || gameObject.XCoordinate < 0;
    }

    public void SpawnAsteroids()
    {
        var randomEdge = _random.Next(0, Edges.Count);
        var edge = Edges[randomEdge];
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

        Asteroids.Add(asteroid);
    }

    private void SplitAsteroid(Asteroid asteroid)
    {
        if (asteroid.Size <= 20)
            return;
        var newAsteroid1 = new Asteroid(
            xCoordinate: asteroid.XCoordinate,
            yCoordinate: asteroid.YCoordinate,
            rotation: asteroid.Rotation + 45,
            size: asteroid.Size / 2,
            velocityX: asteroid.VelocityX
                + Math.Cos(Math.PI * (asteroid.Rotation + 45) / 180.0) * 2,
            velocityY: asteroid.VelocityY + Math.Sin(Math.PI * (asteroid.Rotation + 45) / 180.0) * 2
        );
        var newAsteroid2 = new Asteroid(
            xCoordinate: asteroid.XCoordinate,
            yCoordinate: asteroid.YCoordinate,
            rotation: asteroid.Rotation - 45,
            size: asteroid.Size / 2,
            velocityX: asteroid.VelocityX
                + Math.Cos(Math.PI * (asteroid.Rotation - 45) / 180.0) * 2,
            velocityY: asteroid.VelocityY + Math.Sin(Math.PI * (asteroid.Rotation - 45) / 180.0) * 2
        );
        Asteroids.Add(newAsteroid1);
        Asteroids.Add(newAsteroid2);
    }

    private void CheckCollisions()
    {
        var asteroidsToSplit = new List<Asteroid>();
        var asteroidsToRemove = new List<Asteroid>();
        var projectilesToRemove = new List<Projectile>();
        foreach (var asteroid in Asteroids)
        {
            foreach (var shipEntry in Ships)
            {
                var ship = shipEntry.Value;

                if (ship.CheckCollision(asteroid))
                {
                    ship.Health -= asteroid.Size;
                    asteroidsToSplit.Add(asteroid);
                    asteroidsToRemove.Add(asteroid);

                    if (ship.Health <= 0)
                    {
                        Ships.Remove(shipEntry.Key);
                    }
                }
            }
        }

        foreach (var projectile in Projectiles)
        {
            foreach (
                var asteroid in Asteroids.Where(asteroid => projectile.CheckCollision(asteroid))
            )
            {
                asteroidsToSplit.Add(asteroid);
                asteroidsToRemove.Add(asteroid);
                projectilesToRemove.Add(projectile);
            }
        }

        foreach (var asteroid in asteroidsToSplit)
        {
            SplitAsteroid(asteroid);
        }
        foreach (var asteroid in asteroidsToRemove)
        {
            Asteroids.Remove(asteroid);
        }
        foreach (var projectile in projectilesToRemove)
        {
            Projectiles.Remove(projectile);
        }
    }
}

public static class GameExtensions
{
    public static GameSnapShot ToGameSnapShot(
        this Game game,
        LobbyStatus currentStatus,
        bool isOwner = false
    )
    {
        var ships = game.GetShips();
        var asteroids = game.GetAsteroids();
        var projectiles = game.GetProjectiles();
        var gameState = new GameSnapShot(
            isOwner: isOwner,
            playerCount: ships.Count,
            currentStatus: currentStatus,
            ships: ships,
            asteroids: asteroids,
            projectiles: projectiles,
            boardWidth: game.BoardWidth,
            boardHeight: game.BoardHeight
        );
        return gameState;
    }
}
