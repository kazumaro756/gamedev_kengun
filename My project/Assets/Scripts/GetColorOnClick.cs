using UnityEngine;

public class GetColorOnClick : MonoBehaviour
{
    void Update()
    {
        // 【追加】タスクパネルが表示されている間は、地図のクリック判定を中止する
        if (UIManager.Instance != null && UIManager.Instance.IsTaskPanelActive)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                SpriteRenderer sr = hit.collider.GetComponent<SpriteRenderer>();

                if (sr != null && sr.sprite != null)
                {
                    Sprite sprite = sr.sprite;
                    Texture2D tex = sprite.texture;

                    Vector3 localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                    int px = Mathf.FloorToInt(localHitPoint.x * sprite.pixelsPerUnit + sprite.pivot.x);
                    int py = Mathf.FloorToInt(localHitPoint.y * sprite.pixelsPerUnit + sprite.pivot.y);
                    px += Mathf.FloorToInt(sprite.textureRect.x);
                    py += Mathf.FloorToInt(sprite.textureRect.y);

                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        Color pixelColor = tex.GetPixel(px, py);
                        string stateKey = ColorUtility.ToHtmlStringRGB(pixelColor);

                        // マネージャーに「この色の州データちょうだい！」と頼む
                        ProvinceData clickedProvince = ProvinceDataManager.Instance.GetProvince(stateKey);

                        if (clickedProvince != null)
                        {
                            Debug.Log($"クリックされた州: {clickedProvince.stateName} / 人口: {clickedProvince.population} / 所有者: {clickedProvince.owner}");
                            UIManager.Instance.UpdateProvinceUI(clickedProvince);
                        }
                        else
                        {
                            Debug.Log($"未登録の色キーがクリックされました: {stateKey}");
                        }
                    }
                }
            }
        }
    }
}