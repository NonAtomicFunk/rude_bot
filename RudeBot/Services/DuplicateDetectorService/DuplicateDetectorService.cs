﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Services.DuplicateDetectorService
{
    public class DuplicateDetectorService : IDuplicateDetectorService
    {
        private TimeSpan _expireTime;
        private float _gain;

        public DuplicateDetectorService(TimeSpan expireTime, float gain)
        {
            _expireTime = expireTime;
            _gain = gain;

            _cache = new Dictionary<long, List<DuplicateDetectorMessageDescriptor>>();
        }

        private Dictionary<long, List<DuplicateDetectorMessageDescriptor>> _cache;
        public List<int> FindDuplicates(long chatId, int messageId, string text)
        {
            List<int> epmtyResult = new List<int>();

            if (text is null || text.Length < 20)
            {
                return epmtyResult;
            }

            // Lock for 1 running instance per time
            lock (this)
            {
                if (_cache.TryGetValue(chatId, out var descriptors))
                {
                    // Remove expired items
                    descriptors = descriptors
                        .Where(x => x.Expires > DateTime.UtcNow)
                        .ToList();

                    // Try find similar messages
                    var similarPosts = descriptors
                        .Where(x => x.Equals(text, _gain))
                        .ToList();

                    // Update expire time (not works)
                    //similarPosts
                    //    .ForEach(x => x.Expires += _expireTime);

                    return similarPosts
                        .Select(x => x.MessageId)
                        .ToList();
                }
                else
                {
                    // Add nerw chat and message descriptor
                    descriptors = new List<DuplicateDetectorMessageDescriptor>() {
                        new DuplicateDetectorMessageDescriptor
                        {
                            Text = text,
                            MessageId = messageId,
                            Expires = DateTime.UtcNow + _expireTime
                        }
                    };

                    _cache[chatId] = descriptors;
                }
            }

            return epmtyResult;
        }
    }
}
