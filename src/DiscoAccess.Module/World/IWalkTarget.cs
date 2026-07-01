using DiscoAccess.Core.World;
using Vector3 = System.Numerics.Vector3;

namespace DiscoAccess.Module.World
{
    /// <summary>
    /// A world thing the Enter verb can walk to and act on: the sensing contract (<see cref="IWorldItem"/>)
    /// plus the two extra facts <see cref="WalkInteract"/> needs beyond it - the approach stand-point with a
    /// facing, and the arrival-range test. Both proxies implement it (<see cref="EntityProxy"/> through the
    /// game's own interaction location and radius, <see cref="OrbProxy"/> through the orb body and its
    /// interaction circle), so the walk verb drives entities and orbs down one path.
    /// </summary>
    internal interface IWalkTarget : IWorldItem
    {
        /// <summary>The stand-point to walk to and the heading to face on arrival, computed from
        /// <paramref name="from"/> (the character's current position).</summary>
        Vector3 Approach(Vector3 from, out float heading);

        /// <summary>Whether the character at <paramref name="playerPos"/> stands close enough to act.</summary>
        bool WithinInteractionRadius(Vector3 playerPos);

        /// <summary>The line to speak right after a successful <see cref="IWorldItem.Interact"/>, or null when
        /// nothing extra is said. An entity is silent (the game reacts on its own); a simple orb returns the
        /// clue text it floats into the world, which no other reader carries, so the mod voices it itself.</summary>
        string PostInteractLine();
    }
}
