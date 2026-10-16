using System.Collections.Generic;
using UnityEngine;

public class HeartUI : MonoBehaviour
{
    [SerializeField] private GameObject heartPrefab;
    private List<GameObject> hearts = new List<GameObject>();

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        while (hearts.Count < maxHealth)
        {
            GameObject newHeart = Instantiate(heartPrefab, transform);
            hearts.Add(newHeart);
        }

        while (hearts.Count > maxHealth)
        {
            Destroy(hearts[hearts.Count - 1]);
            hearts.RemoveAt(hearts.Count - 1);
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            Transform redHeart = hearts[i].transform.Find("RedHeart");
            redHeart.gameObject.SetActive(i < currentHealth);
        }
    }
}
