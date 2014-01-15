using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HCI.GAS.Main.Event
{
	public interface IMessageBus
	{
		void Subscribe<TMessage>(Action<TMessage> handler);
		void Unsubscribe<TMessage>(Action<TMessage> handler);
		void Publish<TMessage>(TMessage message);
		void Publish(Object message);
	}
}
