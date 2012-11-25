using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.Collections;

public class ARAstarHeapNode
{
	public ARAstarNode node;
	public int index;
	public int parent;
	public int leftChild;
	public int rightChild;
	
	public ARAstarHeapNode(ARAstarNode _node, int _index)
	{
		node = _node;
		index = _index;
		leftChild = 2*index+1;
		rightChild = 2*index+2;
		parent = index == 0 ? -1 : index/2;
	}
		
	
}

public class ARAstarHeap
{
	public List<ARAstarHeapNode> heap;
	public float currentWeight;
	
	public ARAstarHeap(float inflationFactor)
	{
		heap = new List<ARAstarHeapNode>();
		currentWeight = inflationFactor;
	}
	
	private void SwapNodes(int i, int j)
	{
		ARAstarNode temp = heap[i].node;
		heap[i].node = heap[j].node;
		heap[j].node = temp; 
	}
	
	public ARAstarNode HeapMaximum()
	{
		return heap[0].node;	
	}
	
	public ARAstarNode ExtractMax()
	{
		if(heap.Count == 0)
			throw new InvalidOperationException("Empty heap.");
		ARAstarNode max = heap[0].node;
		heap[0].node = heap[heap.Count-1].node;
		heap.RemoveAt(heap.Count-1);
		HeapDown(0);
		return max;		
	}
	
	public void Insert(ARAstarNode node)
	{
		ARAstarHeapNode newHeapNode = new ARAstarHeapNode(node, heap.Count);
		heap.Add(newHeapNode);
		HeapUp(heap.Count-1);
	}
	
	public int Count()
	{
		return heap.Count;	
	}
	
	public void Remove(ARAstarNode node)
	{
		foreach(ARAstarHeapNode n in heap)
		{
			if(n.node == node){
				int index = heap.IndexOf(n);
				SwapNodes(index, heap.Count-1);
				heap.RemoveAt(heap.Count-1);
				HeapDown(index);
				break;
			}
		}
	}
	
	
	public void Heapify()
	  {
	    for (int i = heap[heap.Count - 1].parent; i >= 0; i--)
	    {
	      HeapDown(i);
	    }
	  }
	 
	  private void HeapUp(int i)
	  {
	    ARAstarNode node = heap[i].node;
	    while (true)
	    {
	      int parent = heap[i].parent;
	      if (parent < 0 || compareKey(heap[parent].node, node)==1) 
				break;
	      SwapNodes(i, parent);
	      i = parent;
	    }
	  }
	 
	  private void HeapDown(int i)
	  {
		if(i >= heap.Count) return;
	    while (true)
	    {
	      int lchild = heap[i].leftChild;
	      int rchild = heap[i].rightChild;

		  int child;
		 if(lchild > heap.Count-1) break;
		 else if(rchild > heap.Count-1) break;
		 else{
		   child = compareKey(heap[lchild].node, heap[rchild].node) == 1 ? lchild : rchild;
		}
	 
	      if (compareKey(heap[child].node, heap[i].node) == -1) break;
	      SwapNodes(i, child);
	      i = child;
	    }
	  }
	
	int compareKey(ARAstarNode firstNode, ARAstarNode secondNode)
	{
		float[] firstKey = firstNode.Key(currentWeight);
		float[] secondKey = secondNode.Key(currentWeight);
		if(firstKey[0] < secondKey[0])
			return 1;
		else if(firstKey[0] > secondKey[0])
			return -1;
		else{
			if(firstKey[1] < secondKey[1])
				return 1;
			else 
				return -1;
		}
	}
}
	