using System.Collections;
using UnityEngine;

public class ReturnCube : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(ReturnCubeIE());
    }

    IEnumerator ReturnCubeIE()
    {
        yield return new WaitForSeconds(2f);
        GameManager.Pool.Release(this.gameObject);
    }

}
