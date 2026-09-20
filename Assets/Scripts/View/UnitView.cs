using SparkAge.Config;
using SparkAge.Controller.Network;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Orders;
using SparkAge.Model.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;

namespace SparkAge.View
{
    /// <summary>
    /// 单位表现层
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        GameState state;
        float hexSize;

        //独占字段
        Dictionary<Unit, GameObject> unitObjs = new Dictionary<Unit, GameObject>();// 单位->游戏对象的映射
        public Dictionary<Unit, GameObject> UnitObjs => unitObjs;

        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;
        }

        /// <summary>
        /// 创建单位对象
        /// </summary>
        public GameObject BuildUnit(Unit unit)
        {
            UnitCfg cfg = null;
            foreach(var unitCfg in ConfigMgr.Instance.unitCfgs)
                if(unitCfg.Type == unit.Type)
                    cfg = unitCfg;
            GameObject unitObj = Instantiate(cfg.Prefab);
            unitObj.transform.Find("Marker").GetComponent<MeshRenderer>().material =
                new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = ViewTools.GetPlayerColor(unit.Owner)
                };

            unitObjs[unit] = unitObj;
            unitObj.transform.position = HexLayout.HexToPixel(unit.Position, hexSize, 0.5f);
            return unitObj;
        }
        /// <summary>
        /// 更新单位对象
        /// </summary>
        /// <param name="unit"></param>
        public void UpdateUnit(Unit unit)
        {
            if(!unitObjs.TryGetValue(unit, out GameObject unitObj))
            {
                unitObj = BuildUnit(unit);
            }
            unitObj.transform.position = HexLayout.HexToPixel(unit.Position, hexSize, 0.5f);
        }
        /// <summary>
        /// 销毁单位对象
        /// </summary>
        /// <param name="unit"></param>
        public void DestroyUnit(Unit unit)
        {
            Destroy(unitObjs[unit]);
            unitObjs.Remove(unit);
        }

        private WaitForSeconds moveDeltaTime = new WaitForSeconds(0.5f);
        IEnumerator WalkSteps(Unit unit, List<HexCoord> path, int endIdx)
        {
            Transform unitTrans = unitObjs[unit].transform;
            for (int i = 0; i <= endIdx; i++)
            {
                Vector3 target = HexLayout.HexToPixel(path[i], hexSize, 0.5f);

                Vector3 dir = target - unitTrans.position;
                dir.y = 0;

                unitTrans.rotation = Quaternion.LookRotation(dir);
                while (Vector3.Distance(unitTrans.position, target) > 0.01f)
                {
                    unitTrans.position = Vector3.MoveTowards(
                        unitTrans.position,
                        target,
                        4 * Time.deltaTime 
                    );
                    yield return null;
                }
                unitTrans.position = target;
            }
        }
        IEnumerator AttackAnimation(Unit unit, HexCoord target)
        {
            yield return null;
        }

        /// <summary>
        /// 单位移动
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="tarHex"></param>
        public void MoveUnit(Unit unit, List<HexCoord> path)
        {
            StartCoroutine(MoveUnitSequence(unit, path));
        }

        /// <summary>
        /// 单位移动协程，移动动画
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerator MoveUnitSequence(Unit unit, List<HexCoord> path)
        {
            yield return StartCoroutine(WalkSteps(unit, path, path.Count - 1));
            
            if(state.CurrentPlayer == NetworkMgr.Instance.MyPlayerId)
                EventCenter.Instance.EventTrigger<MoveUnitEvent>(new MoveUnitEvent(unit));
        }
        public void AttackUnit(Unit attacker, Unit defender, bool canEnter, List<HexCoord> path)
        {
            StartCoroutine(AttackUnitSequence(attacker, defender, canEnter, path));
        }

        /// <summary>
        /// 攻击单位协程，移动+攻击动画
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerator AttackUnitSequence(Unit attacker, Unit defender, bool canEnter, List<HexCoord> path)
        {
            //靠近目标单位
            yield return StartCoroutine(WalkSteps(attacker, path, path.Count - 2));

            //停顿1秒暂且当做攻击动画
            yield return new WaitForSeconds(1f);

            if (!attacker.IsDead && defender.IsDead && canEnter)
                unitObjs[attacker].transform.position = HexLayout.HexToPixel(path[path.Count - 1], hexSize, 0.5f);
            
            if (attacker.IsDead)
                DestroyUnit(attacker);
            if (defender.IsDead)
                DestroyUnit(defender);

            if (state.CurrentPlayer == NetworkMgr.Instance.MyPlayerId)
                EventCenter.Instance.EventTrigger<AttackUnitEvent>(new AttackUnitEvent(attacker, defender));
        }

        /// <summary>
        /// 攻击城市协程，移动+攻击动画
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="city"></param>
        /// <param name="cityIsCaptured"></param>
        /// <param name="path"></param>
        /// <param name="defenderIsDead"></param>
        public void AttackCity(Unit attacker, City city, bool cityIsCaptured, List<HexCoord> path)
        {
            StartCoroutine(AttackCitySequence(attacker, city, cityIsCaptured, path));
        }
        IEnumerator AttackCitySequence(Unit attacker, City city, bool cityIsCaptured, List<HexCoord> path)
        {
            //靠近目标单位
            yield return StartCoroutine(WalkSteps(attacker, path, path.Count - 2));
            //停顿1秒暂且当做攻击动画
            yield return new WaitForSeconds(1f);

            if (cityIsCaptured)
            {
                unitObjs[attacker].transform.position = HexLayout.HexToPixel(path[path.Count - 1], hexSize, 0.5f);
            }

            if (state.CurrentPlayer == NetworkMgr.Instance.MyPlayerId)
                EventCenter.Instance.EventTrigger<AttackCityEvent>(new AttackCityEvent(attacker, city, cityIsCaptured));
        }
    }
}