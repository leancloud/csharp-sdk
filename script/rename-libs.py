# coding=utf8
import os
import re

NAMESPACE_PREFIX = 'LC'

LIB_NAMES = [
  'Newtonsoft',
  'Google'
]

SDK_ROOT = '../'

REPLACES = {}

for name in LIB_NAMES:
  REPLACES['namespace %s' % name] = 'namespace %s.%s' % (NAMESPACE_PREFIX, name)
  REPLACES['using %s' % name] = 'using %s.%s' % (NAMESPACE_PREFIX, name)
  REPLACES['global::%s' % name] = 'global::%s.%s' % (NAMESPACE_PREFIX, name)
  REPLACES['using static %s' % name] = 'using static %s.%s' % (NAMESPACE_PREFIX, name)
  REPLACES[' = %s' % name] = ' = %s.%s' % (NAMESPACE_PREFIX, name)

for path, dirs, files in os.walk(SDK_ROOT):
  files = [f for f in files if f.endswith('.cs')]
  for file in files:
    filepath = os.path.join(path, file)
    print(filepath)
    with open(filepath, 'r', encoding='utf-8') as f:
      content = f.read()
    for (k, v) in REPLACES.items():
      regex = r'\b%s\b' % k
      content = re.sub(regex, v, content)
    with open(filepath, 'w') as f:      
      f.write(content)
