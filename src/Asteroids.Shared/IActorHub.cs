﻿namespace Asteroids.Shared;

public interface IActorHub
{
    Task TellActor(string user, string message);
    Task TellClient(string message);
}
