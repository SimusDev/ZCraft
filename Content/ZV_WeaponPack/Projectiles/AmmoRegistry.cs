using System.Collections.Generic;

namespace ZCraft.Source.Content.Survival.Projectiles;

/// <summary>
/// Плотный реестр патронов. Определения хранятся в массиве, идентификатор = индекс в массиве
/// (вар-инт по сети). Заполняется один раз при старте контента, дальше только чтение.
/// </summary>
public static class AmmoRegistry
{
	private static readonly List<AmmoDefinition> _defs = new(32);
	private static readonly Dictionary<string, int> _byId = new(32);

	/// <summary>Количество зарегистрированных патронов.</summary>
	public static int Count => _defs.Count;

	/// <summary>Регистрирует патрон, возвращает его сетевой индекс.</summary>
	public static int Register(in AmmoDefinition def)
	{
		if (_byId.TryGetValue(def.Id, out int existing))
			return existing;

		int index = _defs.Count;
		_defs.Add(def);
		byIdAdd(def.Id, index);
		return index;
	}

	private static void byIdAdd(string id, int index) => _byId[id] = index;

	/// <summary>Индекс патрона по строковому Id; −1 если не найден.</summary>
	public static int GetIndex(string id)
	{
		return _byId.TryGetValue(id, out int index) ? index : -1;
	}

	/// <summary>Данные патрона по индексу. Индекс вне диапазона возвращает дефолт-патрон (индекс 0).</summary>
	public static ref readonly AmmoDefinition Get(int index)
	{
		if (index < 0 || index >= _defs.Count)
			return ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_defs)[0];
		return ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_defs)[index];
	}
}
