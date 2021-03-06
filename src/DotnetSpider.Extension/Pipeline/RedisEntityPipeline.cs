﻿using System;
using System.Collections.Generic;
using System.Linq;
using DotnetSpider.Core;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Extension.Model;
using MessagePack;
using StackExchange.Redis;

namespace DotnetSpider.Extension.Pipeline
{
	public class RedisEntityPipeline : BasePipeline
	{
		private readonly ConnectionMultiplexer _connection;
		private readonly string _key;

		/// <summary>
		/// 构造方法
		/// </summary>
		/// <param name="key"></param>
		/// <param name="connectString">连接字符串</param>
		public RedisEntityPipeline(string key, string connectString)
		{
			_connection = ConnectionMultiplexer.Connect(connectString);
			_key = key;
		}

		public override void Process(IEnumerable<ResultItems> resultItems, ISpider spider)
		{
			if (resultItems == null)
			{
				return;
			}

			var items = new List<dynamic>();
			foreach (var resultItem in resultItems)
			{
				resultItem.Request.CountOfResults = 0;
				resultItem.Request.EffectedRows = 0;

				foreach (var kv in resultItem.Results)
				{
					if (kv.Value is Tuple<IModel, IEnumerable<dynamic>> value && value.Item2 != null && value.Item2.Any())
					{
						items.AddRange(value.Item2);
						resultItem.Request.CountOfResults += value.Item2.Count();
						resultItem.Request.EffectedRows += value.Item2.Count();
					}
				}
			}

			var db = _connection.GetDatabase(0);
			db.ListLeftPush(_key, LZ4MessagePackSerializer.Typeless.Serialize(items));
		}
	}
}