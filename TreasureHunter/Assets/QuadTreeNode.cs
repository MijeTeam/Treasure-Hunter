using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_NODESTATUS
{
    NOT_COMBINED = 0, // 정해지지 않은 노드 ( 자식노드 접근 가능 )
    COMBINED_DEAD = 1, // 정해진 노드_0
    COMBINED_ALIVE = 2, // 정해진 노드_1
}

public class QuadTreeNode
{
    public int level = 0;
    public E_NODESTATUS status = E_NODESTATUS.COMBINED_DEAD;

    public QuadTreeNode parent;
    public QuadTreeNode[] childs = new QuadTreeNode[4];
}
