using SparkAge.Config;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.GameInfos;
using SparkAge.Model.Hex;
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
        //[SerializeField] Material warriorMaterial;
        //[SerializeField] Material settlerMaterial;

        //外部提供字段
        GameState state;
        float hexSize;
        List<UnitCfg> unitCfgs;

        //独占字段
        Dictionary<Unit, GameObject> unitObjs = new Dictionary<Unit, GameObject>();// 单位->游戏对象的映射
        public Dictionary<Unit, GameObject> UnitObjs => unitObjs;

        public void Init(GameState state, float hexSize, List<UnitCfg> unitCfgs)
        {
            this.state = state;
            this.hexSize = hexSize;
            this.unitCfgs = unitCfgs;

            //warriorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            //{
            //    color = Color.red
            //};
            //settlerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            //{
            //    color = Color.blue
            //};
        }

        private void Start()
        {
            //订阅建城事件
            EventCenter.Instance.AddListener<FoundCityEvent>(e =>
            {
                DestroyUnit(e.ConsumedSettler);
            });
            //订阅造兵事件
            EventCenter.Instance.AddListener<BuildUnitEvent>(e =>
            {
                BuildUnit(e.BuiltUnit);
            });
        }

        /// <summary>
        /// 创建单位对象
        /// </summary>
        public GameObject BuildUnit(Unit unit)
        {
            UnitCfg cfg = null;
            foreach(var unitCfg in unitCfgs)
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
            for (int i = 0; i <= endIdx; i++)
            {
                unitObjs[unit].transform.position = HexLayout.HexToPixel(path[i], hexSize, 0.5f);
                yield return moveDeltaTime;
            }
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
            //发布单位移动事件
            EventCenter.Instance.EventTrigger<UnitMoveEvent>(new UnitMoveEvent(unit, path, false));
        }
        public void AttackUnit(Unit attacker, Unit defender, bool attackerIsDead,bool defenderIsDead,List<HexCoord> path)
        {
            StartCoroutine(AttackUnitSequence(attacker, defender, attackerIsDead, defenderIsDead, path));
        }

        /// <summary>
        /// 攻击单位协程，移动+攻击动画
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerator AttackUnitSequence(Unit attacker, Unit defender, bool attackerIsDead, bool defenderIsDead, List<HexCoord> path)
        {
            //靠近目标单位
            yield return StartCoroutine(WalkSteps(attacker, path, path.Count - 2));

            //停顿1秒暂且当做攻击动画
            yield return new WaitForSeconds(1f);

            if (!attackerIsDead && defenderIsDead)
                unitObjs[attacker].transform.position = HexLayout.HexToPixel(path[path.Count - 1], hexSize, 0.5f);
            if (attackerIsDead)
                DestroyUnit(attacker);
            if (defenderIsDead)
                DestroyUnit(defender);

            Debug.Log("战斗结果：\n攻方：" + (attackerIsDead ? "死亡" : "存活") + "；守方：" + (defenderIsDead ? "死亡" : "存活"));

            //发布攻击单位完成事件
            EventCenter.Instance.EventTrigger<AttackUnitEvent>(new AttackUnitEvent(attacker, attackerIsDead));
        }

        /// <summary>
        /// 攻击城市协程，移动+攻击动画
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="city"></param>
        /// <param name="cityIsCaptured"></param>
        /// <param name="path"></param>
        /// <param name="defenderIsDead"></param>
        public void AttackCity(Unit attacker, City city, bool cityIsCaptured, List<HexCoord> path, bool defenderIsDead)
        {
            StartCoroutine(AttackCitySequence(attacker, city, cityIsCaptured, path, defenderIsDead));
        }
        IEnumerator AttackCitySequence(Unit attacker, City city, bool cityIsCaptured, List<HexCoord> path, bool defenderIsDead)
        {
            //靠近目标单位
            yield return StartCoroutine(WalkSteps(attacker, path, path.Count - 2));
            //停顿1秒暂且当做攻击动画
            yield return new WaitForSeconds(1f);

            if (cityIsCaptured)
            {
                unitObjs[attacker].transform.position = HexLayout.HexToPixel(path[path.Count - 1], hexSize, 0.5f);
            }
            if (defenderIsDead)
            {
                //给守方玩家游戏失败的信息（主要是UI层）
                Debug.Log("玩家" + city.Owner.ToString() + "失败");
            }
            Debug.Log("战斗结果：\n城市：" + (cityIsCaptured ? "被攻占" : "受到伤害"));
            //发布攻击城市完成事件
            EventCenter.Instance.EventTrigger<AttackCityEvent>(new AttackCityEvent(attacker, city, cityIsCaptured, defenderIsDead));
        }
    }
}