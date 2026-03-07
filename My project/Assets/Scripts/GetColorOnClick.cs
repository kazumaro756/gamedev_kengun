using UnityEngine;

public class GetColorOnClick : MonoBehaviour
{
    void Update()
    {
        // 左クリック検知
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 3D空間上のコライダー(BoxColliderなど)へのヒットを判定
            if (Physics.Raycast(ray, out hit))
            {
                SpriteRenderer sr = hit.collider.GetComponent<SpriteRenderer>();

                if (sr != null && sr.sprite != null)
                {
                    Sprite sprite = sr.sprite;
                    Texture2D tex = sprite.texture;

                    // 1. ヒットしたワールド座標を、Spriteオブジェクトのローカル座標に変換
                    Vector3 localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);

                    // 2. ローカル座標から、png画像(テクスチャ)上のピクセル座標(X, Y)を計算
                    // (PixelsPerUnitとPivot位置を考慮して計算します)
                    int px = Mathf.FloorToInt(localHitPoint.x * sprite.pixelsPerUnit + sprite.pivot.x);
                    int py = Mathf.FloorToInt(localHitPoint.y * sprite.pixelsPerUnit + sprite.pivot.y);

                    // 3. スプライトが切り抜かれている場合(アトラス化など)のオフセットを加算
                    px += Mathf.FloorToInt(sprite.textureRect.x);
                    py += Mathf.FloorToInt(sprite.textureRect.y);

                    // 4. 計算したピクセルが画像の範囲内にあるかチェック
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        // ピクセルの色を取得
                        Color pixelColor = tex.GetPixel(px, py);
                        Debug.Log($"クリックしたピクセルの色: {pixelColor}");
                        string stateKey = ColorUtility.ToHtmlStringRGB(pixelColor);
                        Debug.Log($"クリックしたピクセルの色: {stateKey}");
                    }
                    else
                    {
                        Debug.Log("テクスチャの範囲外をクリックしました。");
                    }
                }
            }
        }
    }
}