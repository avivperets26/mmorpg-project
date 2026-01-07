using System.Collections.Generic;
using UnityEngine;

public static class CharacterSocketUtility
{
    public struct SocketRig
    {
        public Transform socketsRoot;
        public Transform avatarRoot;
    }

    public static SocketRig EnsureSockets(Transform root)
    {
        if (!root)
            return default;

        var bones = new Dictionary<string, Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            bones[t.name] = t;

        Transform rightHand = FindBone(bones, "Bip001 R Hand", "RightHand", "RightHand_JNT");
        Transform leftHand = FindBone(bones, "Bip001 L Hand", "LeftHand", "LeftHand_JNT");
        Transform head = FindBone(bones, "Bip001 Head", "Head", "Head_JNT");
        Transform spine = FindBone(bones, "Bip001 Spine", "Spine", "Spine1", "Spine_01");
        Transform chest = FindBone(bones, "Bip001 Spine1", "Spine2", "Chest", "Spine_02");
        Transform hips = FindBone(bones, "Bip001 Pelvis", "Hips", "Pelvis");

        CreateSocket("RightHand_Socket", rightHand, root);
        CreateSocket("LeftHand_Socket", leftHand, root);
        CreateSocket("Head_Socket", head, root);

        // Optional MMO-style sockets: use spine/chest/hips as sensible defaults.
        CreateSocket("Wings_Socket", chest ? chest : spine, root);
        CreateSocket("Orb_Socket", chest ? chest : spine, root);
        CreateSocket("Pet_Socket", hips ? hips : root, root);

        return new SocketRig
        {
            socketsRoot = root,
            avatarRoot = root
        };
    }

    private static Transform FindBone(Dictionary<string, Transform> bones, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (bones.TryGetValue(names[i], out var t))
                return t;
        }
        return null;
    }

    private static void CreateSocket(string socketName, Transform bone, Transform fallbackRoot)
    {
        var parent = bone ? bone : fallbackRoot;
        if (!parent)
            return;

        var existing = parent.Find(socketName);
        if (existing)
            return;

        var go = new GameObject(socketName);
        go.transform.SetParent(parent, false);
    }
}
