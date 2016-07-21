using UnityEngine;
using Layer = Rust.Layer;

namespace Oxide.Classes
{
    public static class PhysicsUtility
    {
        /// <summary>Shoots a ray from the sky towards the ground and returns the first hit that is a valid ground object that the player can stand on. Returns null if none is found.</summary>
        /// <param name="layer">If specified, the hit will only return if the transform is in that layer.</param>
        public static RaycastHit? RaycastGround(Vector2 position, Layer? layer = null)
        {
            RaycastHit hit;
            int terrainLayer = layer != null ? (1 << (int)layer.Value) : ~((1 << (int)Layer.Prevent_Building) | (1 << (int)Layer.Invisible));

            if (!Physics.Raycast(new Ray(new Vector3(position.x, 2000, position.y), Vector3.down), out hit, 5000, terrainLayer))
                return null;

            if (!hit.transform.name.Contains("rock_") && hit.transform.name != "damage" && !hit.transform.name.Contains("/barricades/") && !hit.transform.name.Contains("River") && hit.transform.gameObject.layer != (int)Layer.Water)
            {
                return hit;
            }

            return null;
        }

        public static Quaternion QuaternionFromRaycast(RaycastHit hit)
        {
            var proj = hit.transform.forward - (Vector3.Dot(hit.transform.forward, hit.normal)) * hit.normal;
            var quaternion = Quaternion.LookRotation(proj, hit.normal);
            return quaternion;
        }

        public static GameObject GetLookTarget(BasePlayer player)
        {
            RaycastHit? hit = GetLookHit(player);

            if (hit == null)
                return null;

            return hit.Value.transform.gameObject;
        }

        public static RaycastHit? GetLookHit(BasePlayer player)
        {
            RaycastHit hit;
            var position = player.eyes.position;
            var direction = player.eyes.HeadForward();
            if (!Physics.Raycast(new Ray(position, direction), out hit, 1000f, ~((1 << (int)Layer.Prevent_Building)) | (1 << (int)Layer.Invisible)))
                return null;

            return hit;
        }
    }
}