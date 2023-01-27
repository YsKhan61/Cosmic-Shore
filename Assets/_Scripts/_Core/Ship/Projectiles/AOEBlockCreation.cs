using System.Collections;
using StarWriter.Core;
using UnityEngine;

public class AOEBlockCreation : AOEExplosion
{
    [SerializeField] Trail trail;
    [SerializeField] float blockCount = 8;
    [SerializeField] float radius = 30f;
    [SerializeField] Vector3 blockScale = new Vector3(20f, 10f, 5f);
    Material blockMaterial;

    public void SetBlockMaterial(Material material)
    {
        blockMaterial = material;
    }

    protected override IEnumerator ExplodeCoroutine()
    {
        yield return new WaitForSeconds(ExplosionDelay);

        for (int i = 0; i < blockCount; i++)
        {
            // Ring One
            var position = transform.position +
                              radius * Mathf.Cos((i / blockCount) * 2 * Mathf.PI) * transform.right +
                              radius * Mathf.Sin((i / blockCount) * 2 * Mathf.PI) * transform.up;
            CreateBlock(position, "::AOE::" + Time.time + "::" + i);
            
            // Ring two
            position = transform.position +
                              1.5f * radius * Mathf.Cos(((i + .5f) / blockCount) * 2 * Mathf.PI) * transform.right +
                              1.5f * radius * Mathf.Sin(((i + .5f) / blockCount) * 2 * Mathf.PI) * transform.up;
            CreateBlock(position, "::AOE::" + Time.time + "::" + i + "-2");
        }

        yield return new WaitForEndOfFrame();
    }

    void CreateBlock(Vector3 position, string ownerId)
    {
        var Block = Instantiate(trail);
        Block.Team = Team;
        Block.ownerId = Ship.Player.PlayerUUID;
        Block.PlayerName = Ship.Player.PlayerName;
        Block.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - transform.position, transform.forward));
        Block.GetComponent<MeshRenderer>().material = blockMaterial;
        Block.ID = Block.ownerId + ownerId;  
        Block.Dimensions = blockScale;
        Block.transform.parent = TrailSpawner.TrailContainer.transform;
    }
}