using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;

namespace RA
{
    public class RobotAnalyzer
    {
        private class UserStatistic
        {
            public int Count { get; set; }
        }

        private readonly TimeSpan _timeThreshold;
        private readonly int _countThreshold;
        private readonly HashSet<int> _robots = new();
        private readonly Dictionary<int, UserStatistic> _counts = new ();
        private readonly LinkedList<(int UserId, DateTime Timestamp)> _events = new ();

        public RobotAnalyzer(TimeSpan timeThreshold, int countThreshold)
        {
            _timeThreshold = timeThreshold;
            _countThreshold = countThreshold;
        }

        public void Add(int userId, DateTime dateTime)
        {
            _events.AddLast((userId, dateTime));

            if (!_counts.TryGetValue(userId, out var userStatistic))
            {
                userStatistic = new UserStatistic{Count = 1};
                _counts.Add(userId, userStatistic);
            }
            else
            {
                ++userStatistic.Count;
            }

            if (userStatistic.Count > _countThreshold)
                _robots.Add(userId);

            CollectOldEvents(dateTime - _timeThreshold);
        }

        private void CollectOldEvents(DateTime threshold)
        {
            // тут исходим из того что вся серия _events упорядочена по времени от меншего к большему

            while (_events.Count > 0)
            {
                var val = _events.First!.Value;

                // Если первый элемент серии еще не вышел за пределы временного окна, то выходим
                if (val.Timestamp >= threshold)
                    break;

                if (_counts.TryGetValue(val.UserId, out var userStatistic))
                {
                    --userStatistic.Count;

                    // Если количество запросов упало и стали не роботом
                    if (userStatistic.Count <= _countThreshold)
                        _robots.Remove(val.UserId);

                    // Если количесво подсчитанных эвентов обнулилось, то перестаем следить за таким юзером
                    if (userStatistic.Count <= 0)
                    {
                        _counts.Remove(val.UserId);
                    }
                }

                _events.RemoveFirst();
            }
        }

        public int Count()
        {
            CollectOldEvents();
            return _robots.Count;
        }
    }
}
