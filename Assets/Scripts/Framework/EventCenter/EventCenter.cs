using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace SparkAge.Framework.EventCenter
{
    /// <summary>
    /// 单例事件中心：支持有餐和无参
    /// </summary>
    public class EventCenter
    {
        private static EventCenter instance = new EventCenter();
        public static EventCenter Instance => instance;
        private EventCenter() { }

        // 存储无参事件
        private Dictionary<string, UnityAction> noParamEvents = new();
        // 存储有参事件：用委托包装可覆盖全类型事件
        private Dictionary<Type, Delegate> paramEvents = new();

        /// <summary>
        /// 订阅无参事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public void AddListener(string eventName, UnityAction action)
        {
            if(noParamEvents.TryGetValue(eventName, out UnityAction existing))
            {
                existing -= action;
                existing += action;
            }
            else
            {
                noParamEvents[eventName] = action;
            }
        }
        /// <summary>
        /// 取消订阅无参事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public void RemoveListener(string eventName, UnityAction action)
        {
            if (noParamEvents.TryGetValue(eventName, out UnityAction existing))
            {
                existing -= action;
            }
        }
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="eventName"></param>
        public void EventTrigger(string eventName)
        {
            if (noParamEvents.TryGetValue(eventName, out UnityAction existing))
            {
                existing?.Invoke();
            }
        }

        /// <summary>
        /// 订阅有参事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public void AddListener<T>(UnityAction<T> action)
        {
            // 从带参事件字典中获取现有委托
            if (paramEvents.TryGetValue(typeof(T), out var existing))
            {
                // 先移除旧委托，再组合新委托，防止重复注册
                paramEvents[typeof(T)] = Delegate.Remove(existing, action);
                paramEvents[typeof(T)] = Delegate.Combine(paramEvents[typeof(T)], action);
            }
            else
            {
                paramEvents[typeof(T)] = action;
            }
        }
        /// <summary>
        /// 取消订阅有参事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public void RemoveListener<T>(UnityAction<T> action)
        {
            if (paramEvents.TryGetValue(typeof(T), out var existing))
            {
                paramEvents[typeof(T)] = Delegate.Remove(existing, action);
            }
        }
        /// <summary>
        /// 发布有参事件
        /// </summary>
        /// <param name="eventName"></param>
        public void EventTrigger<T>(T info)
        {
            if (paramEvents.TryGetValue(typeof(T), out var existing))
            {
                (existing as UnityAction<T>)?.Invoke(info);
            }
        }

        /// <summary>
        /// 清空所有事件
        /// </summary>
        public void Clear()
        {
            paramEvents.Clear();
            noParamEvents.Clear();
        }
    }
}
