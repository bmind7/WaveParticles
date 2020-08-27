using System;
using System.Collections.Generic;
using Unity.Collections;

namespace OneBitLab.Services
{
    public class MessageService : SingletonBase<MessageService>
    {
        //-------------------------------------------------------------
        private readonly Dictionary<Type, ValueType> m_MessageQueues = new Dictionary<Type, ValueType>();

        //-------------------------------------------------------------
        public NativeQueue<T> GetOrCreateMessageQueue<T>() where T : struct
        {
            if( !m_MessageQueues.ContainsKey( typeof(T) ) )
            {
                m_MessageQueues.Add( typeof(T), new NativeQueue<T>( Allocator.Persistent ) );
            }

            return (NativeQueue<T>)m_MessageQueues[ typeof(T) ];
        }

        //-------------------------------------------------------------
        private void OnDestroy()
        {
            foreach( KeyValuePair<Type, ValueType> queue in m_MessageQueues )
            {
                ( queue.Value as IDisposable )?.Dispose();
            }

            m_MessageQueues.Clear();
        }

        //-------------------------------------------------------------
    }
}