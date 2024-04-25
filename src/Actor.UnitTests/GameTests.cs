using Asteroids.Shared.GameEntities;
using FluentAssertions;

namespace Actor.UnitTests.GameTests;

public class AsteroidsTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var asteroid = new Asteroid(100, 150, 90, 30, 5.0, 3.0);

        Assert.Equal(100, asteroid.XCoordinate);
        Assert.Equal(150, asteroid.YCoordinate);
        Assert.Equal(90, asteroid.Rotation);
        Assert.Equal(30, asteroid.Size);
        Assert.Equal(5.0, asteroid.VelocityX);
        Assert.Equal(3.0, asteroid.VelocityY);
    }

    [Fact]
    public void AsteroidsBreak_WhenHitsShips()
    {
        var game = new Game(800, 600);
        var ship = new Ship(100, 100, 0);
        var asteroid = new Asteroid(100, 100, 0, 100, 0, 0);
        game.AddShip("ship1", ship);
        game.Asteroids.Add(asteroid);
        game.Tick();
        game.Asteroids.Count.Should().Be(2);
    }

    [Fact]
    public void Asteroids_HaveUniqueIDs()
    {
        var asteroid1 = new Asteroid(0, 0, 0, 10, 1.0, 1.0);
        var asteroid2 = new Asteroid(0, 0, 0, 10, 1.0, 1.0);

        Assert.NotEqual(asteroid1.Id, asteroid2.Id);
    }

    [Fact]
    public void AsteroidSplitsCorrectly()
    {
        var game = new Game(800, 600);
        var asteroid = new Asteroid(100, 100, 0, 100, 0, 0);
        game.Asteroids.Add(asteroid);
        game.Projectiles.Add(new Projectile(100, 100, 0, 5, 5, 5)); // Positioned at the asteroid
        game.Tick();

        var asteroid1 = game.Asteroids[0];
        var asteroid2 = game.Asteroids[1];
        game.Asteroids.Count.Should().Be(2);
        game.Projectiles.Count.Should().Be(0);
        asteroid1.Size.Should().Be(50);
        asteroid1.XCoordinate.Should().Be(100);
        asteroid1.YCoordinate.Should().Be(100);
        asteroid1.Rotation.Should().Be(asteroid.Rotation + 45);

        asteroid2.YCoordinate.Should().Be(100);
        asteroid2.XCoordinate.Should().Be(100);
        asteroid2.Size.Should().Be(50);
        asteroid2.Rotation.Should().Be(asteroid.Rotation - 45);
    }

    [Fact]
    public void Velocity_AssignmentWorksCorrectly()
    {
        var asteroid = new Asteroid(0, 0, 0, 10, 2.5, 2.5);
        asteroid.VelocityX = 10.0;
        asteroid.VelocityY = 20.0;

        Assert.Equal(10.0, asteroid.VelocityX);
        Assert.Equal(20.0, asteroid.VelocityY);
    }
}

public class GameTest
{
    [Fact]
    public void AddShip_AddsShipCorrectly()
    {
        var game = new Game(800, 600);
        var ship = new Ship(100, 100, 0);
        game.AddShip("ship1", ship);

        Assert.Contains(ship, game.GetShips());
    }

    [Fact]
    public void MoveShips_UpdatesShipPositions()
    {
        var game = new Game(800, 600);
        var ship = new Ship(100, 100, 90);
        game.AddShip("ship1", ship);
        game.UpdateShip("ship1", new UpdateShipParams(true, false, false, false));
        game.Tick(); // This should move the ship
        game.Tick(); // This should move the ship again
        game.Tick(); // This should move the ship again

        Assert.NotEqual(100, ship.YCoordinate); // Assuming the ship moves along the X-axis
    }

    [Fact]
    public void CheckCollisions_DetectsCollisionBetweenShipAndAsteroid()
    {
        var game = new Game(800, 600);
        var ship = new Ship(100, 100, 0);
        var asteroid = new Asteroid(101, 101, 0, 10, 0, 0); // Positioned close enough to collide
        game.AddShip("ship1", ship);
        game.Asteroids.Add(asteroid);

        game.Tick(); // This should check for collisions

        Assert.True(ship.Health < 500); // Assuming initial health is 500 and collision decreases it
    }

    [Fact]
    public void SpawnAsteroids_AddsAsteroidToGame()
    {
        var game = new Game(800, 600);
        game.SpawnAsteroids();

        Assert.Single(game.GetAsteroids());
    }

    [Fact]
    public void MoveAsteroids_RemovesOutOfBoundsAsteroids()
    {
        var game = new Game(800, 600);
        var asteroid = new Asteroid(801, 601, 0, 10, 5, 5); // Positioned out of bounds
        game.GetAsteroids().Add(asteroid);

        game.Tick(); // This should remove out-of-bounds asteroids

        Assert.Empty(game.GetAsteroids());
    }
}

public class ShipTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var ship = new Ship(200, 250, 45);

        Assert.Equal(200, ship.XCoordinate);
        Assert.Equal(250, ship.YCoordinate);
        Assert.Equal(45, ship.Rotation);
        Assert.Equal(500, ship.Health); // Default health
        Assert.Equal(20, ship.Size); // Default size
        Assert.Equal(0, ship.VelocityX); // Default velocity X
        Assert.Equal(0, ship.VelocityY); // Default velocity Y
    }

    [Fact]
    public void CheckCollision_ReturnsTrue_WhenAsteroidsOverlap()
    {
        // Arrange
        var asteroid1 = new Asteroid(100, 100, 0, 50, 0, 0); // Center at (100, 100) with radius 25
        var asteroid2 = new Asteroid(120, 120, 0, 30, 0, 0); // Center at (120, 120) with radius 15

        // Act
        bool collision = CheckCollision(asteroid1, asteroid2);

        // Assert
        Assert.True(collision);
    }

    [Fact]
    public void CheckCollision_ReturnsFalse_WhenAsteroidsDoNotOverlap()
    {
        // Arrange
        var asteroid1 = new Asteroid(100, 100, 0, 50, 0, 0); // Center at (100, 100) with radius 25
        var asteroid2 = new Asteroid(200, 200, 0, 30, 0, 0); // Center at (200, 200) with radius 15

        // Act
        bool collision = CheckCollision(asteroid1, asteroid2);

        // Assert
        Assert.False(collision);
    }

    [Fact]
    public void ShipSlowsDown()
    {
        var game = new Game(800, 600);
        var ship = new Ship(100, 100, 90);
        game.AddShip("ship1", ship);
        game.UpdateShip("ship1", new UpdateShipParams(true, false, false, false));
        game.Tick(); // This should move the ship
        game.Tick(); // This should move the ship again
        ship.VelocityX.Should().BeGreaterThan(0);
        ship.VelocityY.Should().BeGreaterThan(0);
        var movingXVelocity = ship.VelocityX;
        var movingYVelocity = ship.VelocityY;

        game.UpdateShip("ship1", new UpdateShipParams(false, false, false, false));
        game.Tick(); // This should slow down the ship

        ship.VelocityX.Should().BeLessThan(movingXVelocity);
        ship.VelocityY.Should().BeLessThan(movingYVelocity);
    }

    private bool CheckCollision(Asteroid a1, Asteroid a2)
    {
        double distance = Math.Sqrt(
            Math.Pow(a2.XCoordinate - a1.XCoordinate, 2)
                + Math.Pow(a2.YCoordinate - a1.YCoordinate, 2)
        );
        return distance < (a1.Size / 2 + a2.Size / 2);
    }
}

public class ProjectileTests
{
    [Fact]
    public void BulletsShot_1()
    {
        var game = new Game(800, 600);
        var ship = new Ship(100, 100, 90);
        game.AddShip("ship1", ship);
        game.UpdateShip("ship1", new UpdateShipParams(false, false, false, true));
        game.Tick(); // This should move the ship

        game.Projectiles.Count.Should().Be(1); // Assuming the ship moves along the X-axis
    }

    [Fact]
    public void BulletsShot_2()
    {
        var game = new Game(800, 600);
        var ship = new Ship(100, 100, 90);
        game.AddShip("ship1", ship);
        game.UpdateShip("ship1", new UpdateShipParams(false, false, false, true));
        game.Tick(); // This should move the ship
        game.Tick(); // This should move the ship again

        game.Projectiles.Count.Should().Be(2); // Assuming the ship moves along the X-axis
    }

    [Fact]
    public void Bullets_BreakAsteroid()
    {
        var game = new Game(800, 600);
        game.Asteroids.Add(new Asteroid(100, 100, 0, 100, 0, 0));
        game.Projectiles.Add(new Projectile(100, 100, 0, 5, 5, 5)); // Positioned at the asteroid
        game.Tick(); // This should move the ship

        game.Asteroids.Count.Should().Be(2); // Assuming the ship moves along the X-axis
        game.Projectiles.Count.Should().Be(0); // Assuming the ship moves along the X-axis
    }

    [Fact]
    public void BulletsDespawn_OutOfBounds()
    {
        var game = new Game(800, 600);
        game.Projectiles.Add(new Projectile(801, 601, 0, 5, 5, 5));
        game.Tick();

        game.Projectiles.Count.Should().Be(0);
    }

    [Fact]
    public void BulletsDespawn_FlyOutOfBounds()
    {
        var game = new Game(800, 600);
        game.Projectiles.Add(new Projectile(799, 599, 0, 5, 5, 5));
        game.Tick();
        game.Tick();

        game.Projectiles.Count.Should().Be(0);
    }
}
