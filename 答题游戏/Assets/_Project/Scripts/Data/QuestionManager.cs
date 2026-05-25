using UnityEngine;
using System.Collections.Generic;
using OriginXR.Core;

namespace OriginXR.Data
{
    /// <summary>
    /// 题库管理器（单例，挂 GameRoot 上）
    /// 负责从 ScriptableObject 加载题目，按需随机抽取
    /// </summary>
    public class QuestionManager : MonoBehaviour
    {
        [Header("题库资源")]
        [SerializeField] private QuestionBankSO _defaultBank;

        // === 单例 ===
        public static QuestionManager Instance { get; private set; }

        // === 内部 ===
        private List<QuestionData> _allQuestions = new List<QuestionData>();
        private System.Random _random = new System.Random();
        private bool _isLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadQuestions();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>加载题库</summary>
        public void LoadQuestions()
        {
            _allQuestions.Clear();

            if (_defaultBank != null && _defaultBank.questions.Count > 0)
            {
                foreach (var entry in _defaultBank.questions)
                {
                    _allQuestions.Add(entry.ToQuestionData());
                }
                _isLoaded = true;
                Debug.Log($"[QuestionManager] 已从资源加载 {_allQuestions.Count} 道题目");
            }
            else
            {
                // 题库资源为空或没配题 → 用内置兜底题
                CreateFallbackQuestions();
            }
        }

        /// <summary>随机抽取 N 道题（不重复）</summary>
        public List<QuestionData> GetRandomQuestions(int count)
        {
            if (!_isLoaded) LoadQuestions();

            var pool = new List<QuestionData>(_allQuestions);
            var result = new List<QuestionData>();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = _random.Next(pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return result;
        }

        /// <summary>按难度抽取 N 道题</summary>
        public List<QuestionData> GetQuestionsByDifficulty(int count, int minDifficulty, int maxDifficulty)
        {
            if (!_isLoaded) LoadQuestions();

            var pool = _allQuestions.FindAll(q =>
                q.difficulty >= minDifficulty && q.difficulty <= maxDifficulty);

            var result = new List<QuestionData>();
            var tempPool = new List<QuestionData>(pool);

            for (int i = 0; i < count && tempPool.Count > 0; i++)
            {
                int index = _random.Next(tempPool.Count);
                result.Add(tempPool[index]);
                tempPool.RemoveAt(index);
            }

            return result;
        }

        /// <summary>获取题库总数</summary>
        public int GetTotalCount() => _allQuestions.Count;

        /// <summary>获取所有题目</summary>
        public List<QuestionData> GetAllQuestions() => _allQuestions;

        // === 兜底：编辑器里没配题库时用的内置题 ===

        private void CreateFallbackQuestions()
        {
            _allQuestions = new List<QuestionData>
            {
                new QuestionData { id="q1", type=QuestionType.SingleChoice, content="Unity 中控制角色移动的组件是？", difficulty=1, timeLimit=10, explanation="CharacterController 提供 Move() 方法。", devCorrectAnswer="A",
                    options = new List<OptionData> { new OptionData{key="A",content="CharacterController"}, new OptionData{key="B",content="BoxCollider"}, new OptionData{key="C",content="Rigidbody"}, new OptionData{key="D",content="MeshRenderer"} } },
                new QuestionData { id="q2", type=QuestionType.SingleChoice, content="C# 中定义接口的关键字是？", difficulty=1, timeLimit=10, explanation="interface 定义接口，只含声明不含实现。", devCorrectAnswer="B",
                    options = new List<OptionData> { new OptionData{key="A",content="class"}, new OptionData{key="B",content="interface"}, new OptionData{key="C",content="struct"}, new OptionData{key="D",content="abstract"} } },
                new QuestionData { id="q3", type=QuestionType.TrueFalse, content="Time.deltaTime 表示上一帧到当前帧的时间间隔。", difficulty=1, timeLimit=8, explanation="正确，常用于平滑运动。", devCorrectAnswer="T",
                    options = new List<OptionData> { new OptionData{key="T",content="正确"}, new OptionData{key="F",content="错误"} } },
                new QuestionData { id="q4", type=QuestionType.SingleChoice, content="以下哪个不是 C# 的值类型？", difficulty=2, timeLimit=10, explanation="string 是引用类型。", devCorrectAnswer="C",
                    options = new List<OptionData> { new OptionData{key="A",content="int"}, new OptionData{key="B",content="float"}, new OptionData{key="C",content="string"}, new OptionData{key="D",content="bool"} } },
                new QuestionData { id="q5", type=QuestionType.SingleChoice, content="GameObject 通过哪个方法查找子对象？", difficulty=2, timeLimit=10, explanation="transform.Find() 按名称查找子Transform。", devCorrectAnswer="B",
                    options = new List<OptionData> { new OptionData{key="A",content="GetChild()"}, new OptionData{key="B",content="transform.Find()"}, new OptionData{key="C",content="GetComponent()"}, new OptionData{key="D",content="FindObject()"} } },
                new QuestionData { id="q6", type=QuestionType.TrueFalse, content="Unity 中协程必须继承 MonoBehaviour 才能使用。", difficulty=2, timeLimit=8, explanation="正确。StartCoroutine 是 MonoBehaviour 的方法。", devCorrectAnswer="T",
                    options = new List<OptionData> { new OptionData{key="T",content="正确"}, new OptionData{key="F",content="错误"} } },
                new QuestionData { id="q7", type=QuestionType.SingleChoice, content="以下哪个是引用类型？", difficulty=2, timeLimit=10, explanation="class 定义的为引用类型。", devCorrectAnswer="B",
                    options = new List<OptionData> { new OptionData{key="A",content="int"}, new OptionData{key="B",content="class对象"}, new OptionData{key="C",content="struct"}, new OptionData{key="D",content="enum"} } },
                new QuestionData { id="q8", type=QuestionType.SingleChoice, content="Unity 的 Awake 在什么时机调用？", difficulty=3, timeLimit=10, explanation="Awake 在脚本实例化时调用，早于 Start。", devCorrectAnswer="B",
                    options = new List<OptionData> { new OptionData{key="A",content="场景加载前"}, new OptionData{key="B",content="脚本实例化时"}, new OptionData{key="C",content="第一帧Update前"}, new OptionData{key="D",content="任意时机"} } },
            };
            _isLoaded = true;
            Debug.Log($"[QuestionManager] 使用兜底题库 ({_allQuestions.Count} 题)");
        }
    }
}
