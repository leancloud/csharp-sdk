using System;

namespace LeanCloud.Test {
    public class JustTest {
        public class Animal {

        }

        public class Dog : Animal {

        }

        public class Walk<T> where T : Animal {
            public virtual T Do() {
                return default;
            }
        }

        public class Run : Walk<Dog> {
            public override Dog Do() {
                return base.Do();
            }
        }

        public JustTest() {
        }
    }
}
