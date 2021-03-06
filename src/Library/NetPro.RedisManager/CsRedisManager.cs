﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetPro.RedisManager
{
	internal class CsRedisManager : IRedisManager
	{
		private readonly RedisCacheOption _option;
		public CsRedisManager(RedisCacheOption option)
		{
			_option = option;
		}

		public T Get<T>(string key)
		{
			Common.CheckKey(key);

			return Common.ConvertObj<T>(RedisHelper.Get(key));
		}

		public async Task<T> GetAsync<T>(string key)
		{
			Common.CheckKey(key);

			return Common.ConvertObj<T>(await RedisHelper.GetAsync(key));
		}

		public object GetByLuaScript(string script, object obj)
		{
			var result = RedisHelper.Eval(script, key: "lock_name", args: obj);
			return result;
		}

		public T GetDistributedLock<T>(string resource, int timeoutSeconds, Func<T> func)
		{
			if (timeoutSeconds <= 0 || string.IsNullOrWhiteSpace(resource))
			{
				throw new ArgumentException($"The timeout is not valid with a distributed lock object--key:{resource}--expiryTime--{timeoutSeconds}");
			}
			using (var lockObject = RedisHelper.Lock($"Lock:{resource?.Trim()}", timeoutSeconds))
			{
				if (lockObject == null)
				{
					return default;
				}
				var result = func();
				lockObject.Unlock();
				return result;
			}
		}

		/// <summary>
		/// 获得使用原生stackexchange.redis的能力，用于pipeline (stackExchange.redis专用，Csredis驱动使用此方法会报异常)
		/// </summary>
		/// <returns></returns>
		public IDatabase GetIDatabase()
		{
			//TODO
			throw new NotImplementedException();
		}

		public T HGet<T>(string key, string field)
		{
			Common.CheckKey(key);

			return RedisHelper.HGet<T>(key, field);
		}

		public async Task<T> HGetAsync<T>(string key, string field)
		{
			Common.CheckKey(key);

			return await RedisHelper.HGetAsync<T>(key, field);
		}

		public bool HSet<T>(string key, string field, T value, int expirationMinute = 1)
		{
			bool isSet = RedisHelper.HSet(key, field, value);
			if (isSet && expirationMinute > 0)
				RedisHelper.Expire(key, TimeSpan.FromMinutes(expirationMinute));
			return isSet;
		}

		public async Task<bool> HSetAsync<T>(string key, string field, T value, int expirationMinute = 1)
		{
			bool isSet = await RedisHelper.HSetAsync(key, field, value);
			if (isSet && expirationMinute > 0)
				await RedisHelper.ExpireAsync(key, TimeSpan.FromMinutes(expirationMinute));
			return isSet;
		}

		public bool IsSet(string key)
		{
			return RedisHelper.Exists(key);
		}

		public bool Remove(string key)
		{
			return RedisHelper.Del(key) > 0 ? true : false;
		}

		public bool Remove(string[] keys)
		{
			return RedisHelper.Del(keys) > 0 ? true : false;
		}

		public bool Set(string key, object data, int? cacheTime = -1)
		{
			TimeSpan? expiry = TimeSpan.FromMinutes(cacheTime.Value);
			int totalSeconds = expiry == null ? -1 : (expiry.Value.TotalSeconds > int.MaxValue ? -1 : Convert.ToInt32(expiry.Value.TotalSeconds));
			return RedisHelper.Set(key, data, totalSeconds);
		}

		public async Task<bool> SetAsync(string key, object data, int? cacheTime)
		{
			TimeSpan? expiry = TimeSpan.FromMinutes(cacheTime.Value);
			int totalSeconds = expiry == null ? -1 : (expiry.Value.TotalSeconds > int.MaxValue ? -1 : Convert.ToInt32(expiry.Value.TotalSeconds));
			return await RedisHelper.SetAsync(key, data, totalSeconds);
		}

		public bool ZAdd<T>(string key, T obj, decimal score)
		{
			return RedisHelper.ZAdd(key, (score, obj)) > 0 ? true : false;
		}

		public List<T> ZRange<T>(string key)
		{
			return RedisHelper.ZRange<T>(key, 0, -1)?.ToList();
		}

		
	}
}
