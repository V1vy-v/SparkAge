using SparkAge.Config;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
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
        GameState state;
        float hexSize;

        //独占字段
        Dictionary<int, GameObject> unitObjs = new Dictionary<int, GameObject>();// 单位->游戏对象的映射
        public Dictionary<int, GameObject> UnitObjs => unitObjs;


        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;

            EventCenter.Instance.AddListener<InitialSettlers>(OnInitialSettlers);
            EventCenter.Instance.AddListener<BuildUnitEvent>(OnBuildUnit);
            EventCenter.Instance.AddListener<UpdateUnitEvent>(OnUpdateUnit);
            EventCenter.Instance.AddListener<RemoveUnitEvent>(OnRemoveUnit);
            EventCenter.Instance.AddListener<MoveUnitStartEvent>(OnMoveUnit);
            EventCenter.Instance.AddListener<AttackUnitStartEvent>(OnAttackUnit);
            EventCenter.Instance.AddListener<AttackCityStartEvent>(OnAttackCity);
        }

        private void OnDestroy()
        {
            EventCenter.Instance.RemoveListener<InitialSettlers>(OnInitialSettlers);
            EventCenter.Instance.RemoveListener<BuildUnitEvent>(OnBuildUnit);
            EventCenter.Instance.RemoveListener<UpdateUnitEvent>(OnUpdateUnit);
            EventCenter.Instance.RemoveListener<RemoveUnitEvent>(OnRemoveUnit);
            EventCenter.Instance.RemoveListener<MoveUnitStartEvent>(OnMoveUnit);
            EventCenter.Instance.RemoveListener<AttackUnitStartEvent>(OnAttackUnit);
            EventCenter.Instance.RemoveListener<AttackCityStartEvent>(OnAttackCity);
        }
        public void OnInitialSettlers(InitialSettlers e)
        {
            foreach (var s in e.Settlers)
                BuildUnit(s);
        }
        public void OnBuildUnit(BuildUnitEvent e)
        {
            BuildUnit(e.Unit);
        }
        public void OnUpdateUnit(UpdateUnitEvent e)
        {
            UpdateUnit(e.Unit);
        }
        public void OnRemoveUnit(RemoveUnitEvent e)
        {
            RemoveUnit(e.Unit);
        }
        public void OnMoveUnit(MoveUnitStartEvent e)
        {
            MoveUnit(e.Unit,e.Path);
        }
        public void OnAttackUnit(AttackUnitStartEvent e)
        {
            AttackUnit(e.Attacker, e.Defender, e.CanEnter, e.Path);
        }
        public void OnAttackCity(AttackCityStartEvent e)
        {
            AttackCity(e.Attacker, e.City, e.CityIsCapture, e.Path, e.DefenderUnits);
        }

        //================== 执行方法 =================
        /// <summary>
        /// 创建单位对象
        /// </summary>
        private GameObject BuildUnit(Unit unit)
        {
            UnitCfg cfg = null;
            foreach(var unitCfg in ConfigMgr.Instance.unitCfgs)
                if(unitCfg.Type == unit.Type)
                    cfg = unitCfg;
            GameObject unitObj = Instantiate(cfg.Prefab);
            unitObjs[unit.ID] = unitObj;
            unitObj.transform.position = HexLayout.HexToPixel(unit.Position, hexSize, 0.5f);
            return unitObj;
        }
        /// <summary>
        /// 更新单位表现
        /// </summary>
        /// <param name="unit"></param>
        private void UpdateUnit(Unit unit)
        {
            if (!unitObjs.TryGetValue(unit.ID, out GameObject unitObj))
            {
                unitObj = BuildUnit(unit);
            }

            unitObj.transform.position =
                HexLayout.HexToPixel(unit.Position, hexSize, 0.5f);
        }
        /// <summary>
        /// 销毁单位对象
        /// </summary>
        /// <param name="unit"></param>
        private void RemoveUnit(Unit unit)
        {
            if (unit == null)
                return;

            if (!unitObjs.TryGetValue(unit.ID, out GameObject obj))
                return;

            if (obj != null)
                Destroy(obj);

            unitObjs.Remove(unit.ID);
        }

        /// <summary>
        /// 单位移动
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="tarHex"></param>
        private void MoveUnit(Unit unit, List<HexCoord> path)
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
            
            EventCenter.Instance.EventTrigger<MoveUnitCompletedEvent>(new MoveUnitCompletedEvent(unit));
        }
        private void AttackUnit(Unit attacker, Unit defender, bool canEnter, List<HexCoord> path)
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

            //攻击动画
            yield return StartCoroutine(AttackAnimation(attacker, defender.Position));

            if (!attacker.IsDead && defender.IsDead && canEnter &&
                unitObjs.TryGetValue(attacker.ID, out GameObject attackerObj) && attackerObj != null)
            {
                attackerObj.transform.position = HexLayout.HexToPixel(defender.Position, hexSize, 0.5f);
            }

            if (attacker.IsDead)
                RemoveUnit(attacker);
            if (defender.IsDead)
                RemoveUnit(defender);

            EventCenter.Instance.EventTrigger<AttackUnitCompletedEvent>(new AttackUnitCompletedEvent(attacker, defender));
        }

        /// <summary>
        /// 攻击城市协程，移动+攻击动画
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="city"></param>
        /// <param name="cityIsCaptured"></param>
        /// <param name="path"></param>
        /// <param name="defenderIsDead"></param>
        private void AttackCity(Unit attacker, City city, bool cityIsCaptured, List<HexCoord> path, List<Unit> defeatedUnits)
        {
            StartCoroutine(AttackCitySequence(attacker, city, cityIsCaptured, path, defeatedUnits));
        }
        IEnumerator AttackCitySequence(Unit attacker, City city, bool cityIsCaptured, List<HexCoord> path, List<Unit> defeatedUnits)
        {
            //靠近目标单位
            yield return StartCoroutine(WalkSteps(attacker, path, path.Count - 2));
            //攻击动画
            yield return StartCoroutine(AttackAnimation(attacker, city.Position));

            if (cityIsCaptured)
            {
                if (unitObjs.TryGetValue(attacker.ID, out GameObject attackerObj) && attackerObj != null)
                {
                    attackerObj.transform.position = HexLayout.HexToPixel(city.Position, hexSize, 0.5f);
                }
            }

            foreach (var unit in defeatedUnits)
            {
                if (unit != null && unitObjs.ContainsKey(unit.ID))
                    RemoveUnit(unit);
            }

            EventCenter.Instance.EventTrigger<AttackCityCompletedEvent>(new AttackCityCompletedEvent(attacker, city, cityIsCaptured));
        }


        //================== 动画 =================
        IEnumerator WalkSteps(Unit unit, List<HexCoord> path, int endIdx)
        {
            if (unit == null || !unitObjs.TryGetValue(unit.ID, out GameObject unitObj) || unitObj == null)
                yield break;

            Transform unitTrans = unitObj.transform;
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
            if (!unitObjs.TryGetValue(unit.ID, out GameObject unitObj) || unitObj == null)
            {
                yield break;
            }

            Transform tr = unitObj.transform;

            Vector3 startPos = tr.position;
            Vector3 targetPos = HexLayout.HexToPixel(target, hexSize, 0.5f);

            Vector3 dir = targetPos - startPos;
            dir.y = 0;

            if (dir.sqrMagnitude > 0.001f)
                tr.rotation = Quaternion.LookRotation(dir);

            Vector3 attackPos = Vector3.Lerp(startPos, targetPos, 0.35f);

            const float lungeTime = 0.12f;
            const float returnTime = 0.15f;

            float time = 0f;

            //快速靠近
            while (time < lungeTime)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / lungeTime);

                tr.position = Vector3.Lerp(startPos, attackPos, t);

                yield return null;
            }
            tr.position = attackPos;

            time = 0f;
            //返回原位
            while (time < returnTime)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / returnTime);

                tr.position = Vector3.Lerp(attackPos, startPos, t);

                yield return null;
            }

            tr.position = startPos;
        }
    }
}