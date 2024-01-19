using UnityEngine;



/*
 *	Class Singleton
 *	Implementation:
 *		public class MyClass :  SingletonBehaviour<MyClass> 
 *		{
 *			protected override bool Awake()
 *			{
 *				if (base.Awake())
 *				{	
 *					//initialize here
 *					return true;
 *				}
 *				else
 *					return false;
 *			}
 *			public void foo()
 *			{//do something...}
 *		}
 *
 *  use : MyClass.Instance.foo() to call a function of the singleton implementation where ever you want
 */
public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{

	
	public static T Instance {get; protected set;}
	
	protected virtual bool Awake()
	{
		if ((Instance == null) || (Instance == this))
		{
			Instance = (T) this;
			GameObject.DontDestroyOnLoad(gameObject);
			return true;
		}
		else
		{
			GameObject.Destroy(gameObject);
			return false;
		}
	}
}

