using UnityEngine;

namespace SparkAge.View
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] float pitch = 50f;//俯仰角
        [SerializeField] float distance = 15f;//相机到关注点的距离
        [SerializeField] float minDistance = 5f, maxDistance = 30f;//相机到关注点的距离上下限
        [SerializeField] float moveSpeed = 0.03f;//水平拖拽速度
        [SerializeField] float scrollSpeed = 2f;//滚轮缩放速度

        //关注点
        Vector3 target;
        //地图中心和边界
        Vector3 center, topRight, bottomLeft;

        void LateUpdate()
        {
            //中键滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
                PerspectiveZoom(scroll);

            //中键拖拽
            if (Input.GetMouseButton(2))
                CameraMove();
        }
        /// <summary>
        /// 摄像机位置初始化：地图中央
        /// </summary>
        public void Init(Vector3 center, Vector3 topRight, Vector3 bottomLeft)
        {
            this.center = center;
            this.topRight = topRight;
            this.bottomLeft = bottomLeft;

            target = center;
            transform.rotation = Quaternion.Euler(pitch, 0, 0);
            transform.position = target - transform.forward * distance;
        }
        /// <summary>
        /// 摄像机移动
        /// </summary>
        private void CameraMove()
        {
            // 鼠标位移 → 世界平面位移（要乘相机朝向，否则旋转后方向错乱）
            Vector3 delta = new Vector3(-Input.GetAxis("Mouse X"), 0, -Input.GetAxis("Mouse Y")) * distance * moveSpeed;
            //限制关注点移动范围
            float posX = Mathf.Clamp(target.x + delta.x, bottomLeft.x, topRight.x);
            float posY = Mathf.Clamp(target.z + delta.z, bottomLeft.z, topRight.z);
            target = new Vector3(posX, target.y, posY);
            //相机实时对准关注点
            transform.position = target - transform.forward * distance;
        }
        /// <summary>
        /// 摄像机缩放
        /// </summary>
        /// <param name="scroll"></param>
        private void PerspectiveZoom(float scroll)
        {
            //摄像机缩放
            distance = Mathf.Clamp(distance - scroll * scrollSpeed, minDistance, maxDistance);
            transform.position = target - transform.forward * distance;
        }
    }

}