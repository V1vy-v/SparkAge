using SparkAge.Core.Hex;
using SparkAge.Core.Map;
using SparkAge.Game;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //按下右键时抓的世界点
    Vector3 _grabPoint;
    //滚轮缩放速度
    private float scrollSpeed = 2f; 
    //地图数据
    [SerializeField] private MapView mapView;
    //地图边界
    Vector2 topRight, bottomLeft;

    void Start()
    {
        (topRight, bottomLeft) = mapView.GetMapBounds();
    }

    void Update()
    {
        //右键拖拽
        if(Input.GetMouseButtonDown(1))
            _grabPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        else if (Input.GetMouseButton(1))
            CameraMove();
        //中键滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
            OrthographicZoom(scroll);
    }

    private void CameraMove()
    {
        Vector3 delta = Camera.main.ScreenToWorldPoint(Input.mousePosition) - _grabPoint;
        //范围限制
        float posX = Mathf.Clamp(transform.position.x - delta.x, bottomLeft.x, topRight.x);
        float posY = Mathf.Clamp(transform.position.y - delta.y, bottomLeft.y, topRight.y);
        transform.position = new Vector3(posX, posY, transform.position.z);
    }
    private void OrthographicZoom(float scroll)
    {
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scroll * scrollSpeed, 5, 15);
    }
}
