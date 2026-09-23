using Mirror;
using SparkAge.Controller.Network;
using SparkAge.Model;
using SparkAge.Model.Ai;
using SparkAge.Model.Orders;
using System.Collections;
using UnityEngine;

namespace SparkAge.Controller.Ai
{
    public class AiDriver
    {
        GameState state;
        GameSession session;
        AiDecider decider;
        WaitForSeconds stepDelay;
        int maxStepsPerTurn;
        bool IsAi(int id) => session.GetControllerType(id) == ControllerType.AI;

        public AiDriver(GameState state, GameSession session, AiDecider decider, float stepDelay = 1f, int maxStepsPerTurn = 100)
        {
            this.state = state;
            this.session = session;
            this.decider = decider;
            this.stepDelay = new WaitForSeconds(stepDelay);
            this.maxStepsPerTurn = maxStepsPerTurn;
        }

        public IEnumerator Run(int firstAiPlayerId)
        {
            if (state.IsGameOver)
                yield break;

            if (!NetworkServer.active)
                yield break;

            if (!IsAi(firstAiPlayerId))
                yield break;

            int aiPlayerId = firstAiPlayerId;
            int steps = 0;

            while (state.CurrentPlayer == aiPlayerId && IsAi(aiPlayerId) && !state.IsGameOver)
            {
                if (steps == 0)
                    decider.Reset(aiPlayerId);

                if (steps >= maxStepsPerTurn)
                {
                    NetworkMgr.Instance.ExecuteAndBroadcast(new EndPhaseOrder(aiPlayerId), null);

                    aiPlayerId = state.CurrentPlayer;
                    steps = 0;

                    if (!IsAi(aiPlayerId))
                        yield break;

                    yield return stepDelay;
                    continue;
                }

                BaseOrder order = decider.Decide();
                if (order == null)
                    order = new EndPhaseOrder(aiPlayerId);

                NetworkMgr.Instance.ExecuteAndBroadcast(order, null);
                steps++;

                if (state.IsGameOver)
                    yield break;

                if (order is EndPhaseOrder)
                {
                    aiPlayerId = state.CurrentPlayer;
                    steps = 0;

                    if (!IsAi(aiPlayerId))
                        yield break;

                    yield return stepDelay;
                    continue;
                }

                yield return stepDelay;
            }
        }
    }
}
