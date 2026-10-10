using UnityEngine;

// 2번 게임: 화면 밖 트리거 사방 데드존 스크립트
public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        FruitObject fruit = collision.GetComponent<FruitObject>();
        if (fruit != null && fruit.gameObject.activeSelf)
        {
            fruit.gameObject.SetActive(false);
            GameStateManager.Instance.AddScore(-30);
        }
    }
}