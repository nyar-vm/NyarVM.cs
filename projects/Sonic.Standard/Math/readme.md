# Sonic.Math 完整设计

## 第一部分：总纲与设计哲学

### 1.1 范畴定位

Sonic.Math 是 Sonic 标准库的核心支柱之一，与 Sonic.Lang（语言核心）和
Sonic.Data（数据）并列。它封装了一切形式化推理、数值计算、符号操作、几何抽象、离散结构和密码学原语。Sonic.Math
的使命是为整个生态系统提供统一、可移植、高性能且类型安全的数学基础设施，同时严守抽象边界，不侵入任何特定应用领域。

Sonic.Math 不是一个数学软件包，而是一个 **数学概念的纯净容器**
。它只包含那些可以被严格形式化、不依赖于特定行业语义的数学结构与算法。它向下屏蔽平台差异，向上为图形学、物理模拟、机器学习、统计分析、密码协议等提供公共语言。

### 1.2 设计原则

1. **抽象优先**：所有功能通过 trait 暴露，允许用户替换具体实现。例如，数值类型只需实现相应的运算符 trait
   即可参与所有算法；图算法并不绑定特定存储结构，而是依赖于 `GraphBase` trait。

2. **零成本抽象**：迭代器、编译时多态（单态化）和值语义保证高级抽象不会带来运行时开销。基础类型如 `Vec2`、`Complex`
   在优化后应与手写标量代码性能一致。

3. **显式命名与无缩写**：所有模块、类型、函数名均使用完整单词，不得使用首字母缩写或行业黑话。例如使用 `Matrix` 而非 `Mat`，使用
   `Permutation` 而非 `Perm`。这确保了可读性和跨领域理解的一致性。

4. **纯数学语境**：不引入任何非数学领域的术语。不存在 `AssetPrice`（资产价格）、`RobotJoint`（机器人关节）或 `NeuralNetwork`
   （神经网络）等类型。一切结构均以数学概念命名：`Polynomial`、`Graph`、`Quaternion`、`Prime`。

5. **边界清晰**：标准库不包含完整的应用框架。例如，不提供求解偏微分方程的完整工具包，但提供数值积分和自动微分的基石；不提供完整的统计模型，但提供分布和优化骨架。

6. **安全性**：密码学原语必须经过审计，提供常量时间实现，并默认防止误用（如密钥必须显式销毁）。几何算法在浮点退化场景下具有明确的误差处理策略。

7. **与 Sonic 生态协同**：所有类型均实现 `Sonic.Lang` 中的核心 trait（`Clone`、`Debug`、`Display`、`PartialEq`、`Eq`、
   `PartialOrd`、`Ord`、`Hash`、`Send`、`Sync`），可与 `Sonic.Data` 的集合和序列化无缝互操作。

### 1.3 总体结构

Sonic.Math 包含以下顶层子模块，每个子模块下又可细分。命名严格遵守单数形式，除 `Combinatorics` 本身即学科名称外，其余皆用单数。

- **Foundation**：基础 trait 与函数
- **Numeric**：数值计算
  - **Scalar**：高级标量
  - **Linear**：线性代数
  - **Tensor**：多维数组
  - **Random**：随机数生成与分布
- **Symbolic**：符号计算
  - **Expression**：符号表达式与模式匹配
  - **Calculus**：符号微积分
  - **Polynomial**：多项式与有理函数
  - **Logic**：逻辑表达式
- **Geometry**：几何抽象
  - **Primitive**：基本形体与网格抽象
  - **Transform**：变换
  - **Intersection**：碰撞检测与距离
- **Calculus**：连续数学与数值分析
  - **AutoDiff**：自动微分
  - **Integration**：数值积分与微分方程
  - **Optimization**：数值优化器骨架
- **Combinatorics**：组合数学
  - **Permutation**：排列与置换
  - **Combination**：组合与子集
  - **Partition**：整数分拆与集合分拆
  - **Generating**：生成函数与数列
- **GraphTheory**：图论
  - **Graph**：图数据结构
  - **Traversal**：遍历
  - **Algorithm**：经典算法
  - **Metric**：图度量
- **NumberTheory**：数论
  - **Prime**：素数测试与筛法
  - **Modular**：模运算
  - **CryptoTool**：密码学数论工具
- **Crypto**：密码学原语
  - **Hash**：哈希函数
  - **Aead**：认证加密
  - **Signature**：数字签名
  - **Kem**：密钥封装
  - **Kdf**：密钥派生
- **Constants**：数学常量

下文将对每一个子模块进行详尽设计。

---

## 第二部分：Foundation（基础）

Foundation 是整个 Sonic.Math 的基石，定义了所有数学类型必须遵循的运算符重载 trait、数值标记 trait 以及作用于内置标量类型的基本数学函数。

### 2.1 运算符 Trait

所有运算符 trait 均定义在 `Sonic.Math.Foundation` 中，并与 `Sonic.Lang` 的内置类型系统集成。它们为后续的向量、矩阵、张量等复合类型提供统一的运算接口。

- `Additive` trait：组合了加法（`add`）和减法（`sub`）的 trait，提供默认的 `Additive::add` 和 `Additive::sub` 方法。对应的运算符是
  `+` 和 `-`。
- `Multiplicative` trait：组合了乘法（`mul`）和除法（`div`）的 trait，提供 `Multiplicative::mul` 和 `Multiplicative::div`
  。对应的运算符是 `*` 和 `/`。
- `Remainder` trait：提供取余运算 `rem`，对应运算符 `%`。
- `Negation` trait：提供一元负号 `neg`，对应运算符 `-`（前置）。
- `Bitwise` trait：组合了按位与、或、异或、左移、右移的操作，适用于整数类型。
- `Power` trait：提供幂运算方法 `pow`（指数为整数）和 `powf`（指数为浮点数），但运算符 `**` 不直接重载，而是通过方法调用以确保类型安全。

这些 trait 均采用关联类型（Associated Type）来指定输出类型。例如，`Additive` trait 定义为：

```rust
trait Additive<Right = Self> {
    type Output;
    fn add(self, right: Right) -> Self::Output;
    fn sub(self, right: Right) -> Self::Output;
}
```

这使得不同量纲的单位在相加时产生编译时错误，而乘法可以改变类型。

### 2.2 数值标记 Trait

标记 trait 用于在编译时约束泛型参数，确保函数仅对特定类别的数值可用。

- `Zero`：提供常数 `ZERO`。
- `One`：提供常数 `ONE`。
- `Min` 和 `Max`：表示该类型有明确的边界值，用于 `clamp` 等操作。
- `Bounded`：组合 `Min` 和 `Max`。
- `Unsigned` 和 `Signed`：标记整数的符号特性。
- `Integer`：标记整数类型（`i8`、`u64` 等）。
- `Float`：标记浮点类型（`f32`、`f64`）。
- `Real`：实数标记，由 `Integer` 和 `Float` 共同实现。
- `ComplexFloat`：标记复数类型。

此外，还有 `Approximate` trait，提供浮点数的近似比较方法（`is_close`），考虑绝对误差和相对误差。

### 2.3 基本数学函数

Foundation 提供一系列自由函数，这些函数对于内置标量类型（`f32`、`f64`、`i32`、`i64` 等）有直接实现，但也可通过 trait 扩展至其他类型。

- **绝对值与符号**：`absolute`、`signum`、`copy_sign`。
- **极值**：`minimum`、`maximum`、`clamp`。
- **舍入**：`floor`、`ceiling`、`round`、`truncate`、`fraction`。
- **指数与对数**：`square_root`、`cube_root`、`natural_exponential`、`natural_logarithm`、`logarithm_base`、`power`。
- **三角函数**：`sine`、`cosine`、`tangent`、`arc_sine`、`arc_cosine`、`arc_tangent`、`arc_tangent2`。
- **双曲函数**：`hyperbolic_sine`、`hyperbolic_cosine`、`hyperbolic_tangent`。
- **误差与伽马函数**：`error_function`、`complementary_error_function`、`gamma`、`log_gamma`
  （这些属于特殊函数，但作为基础函数提供，因为它们在统计和物理中极为常见）。
- **整数专用**：`greatest_common_divisor`、`least_common_multiple`、`is_power_of_two`、`next_power_of_two`、`trailing_zeros`、
  `leading_zeros`、`population_count`、`reverse_bits`。

所有函数均可通过 `const` 上下文调用，只要参数是编译时常量。这得益于 `const fn` 的支持。

---

## 第三部分：Numeric（数值计算）

### 3.1 Scalar（高级标量）

Scalar 子模块提供超越内置标量的数值类型，它们都是具有明确数学语义的结构体，并实现了 Foundation 中定义的所有相关 trait。

#### 3.1.1 复数（Complex）

```rust
struct Complex<Real> {
    real: Real,
    imaginary: Real,
}
```

- 提供：`new`、`from_polar`、`magnitude`、`phase`、`conjugate`。
- 实现所有运算符，使得 `Complex<f64>` + `Complex<f64>` 等工作。
- 实现 `Float` 相关标记 trait，允许 `Complex` 自身作为 `Real` 参数嵌套（产生 `Complex<Complex<f64>>`，即双复数）。
- 提供 `sine`、`cosine`、`exponential` 等函数的复数版本，这些函数自动利用标准复数恒等式。

#### 3.1.2 大整数（BigInteger 与 BigUnsignedInteger）

- `BigInteger`：任意精度有符号整数。
- `BigUnsignedInteger`：任意精度无符号整数。
- 内部存储为一个包含多个 `u64` 肢节的动态数组。
- 实现所有算术运算、位运算、比较。
- 提供数论方法：`modular_power`、`modular_inverse`、`is_probably_prime`（Miller-Rabin）、`next_prime`。
- 与内置整数通过 `From`/`TryFrom` 转换，转换失败时返回 `Result` 或截断。

#### 3.1.3 有理数（Rational）

```rust
struct Rational<Int> {
    numerator: Int,
    denominator: Int,
}
```

- 自动约分：在构造函数和每次运算后通过 `greatest_common_divisor` 约简。
- 实现所有算术运算，保证分母不为零，遇零分母会触发 panic 或返回错误（取决于选择）。
- 提供 `to_float` 转换为近似浮点数。

#### 3.1.4 十进制浮点（Decimal64、Decimal128）

- 遵循 IEEE 754-2008 十进制浮点标准。
- 用于金融计算和需要十进制精度的场景。
- 提供与字符串的精确双向转换，不经过二进制中间态。
- 支持 `scale` 和 `precision` 查询。

#### 3.1.5 定点数（FixedPoint）

- 模板结构 `FixedPoint<Base, FractionBits>`，其中 `Base` 为底层整数类型，`FractionBits` 是编译时整数，表示小数部分位数。
- 适用于嵌入式或游戏物理，无浮点非确定性。
- 实现所有算术，乘法/除法后自动调整小数位（截断或四舍五入）。

### 3.2 Linear（线性代数）

Linear 子模块提供面向几何和科学计算的向量、矩阵、四元数等类型，兼顾高性能和泛用性。

#### 3.2.1 向量（Vector）

- 具体类型：`Vec2<T>`、`Vec3<T>`、`Vec4<T>`。
- 通用类型：`Vector<T, N>`，其中 `N` 为编译时常量（通过 const generics）。
- 分量访问：通过 `.x`、`.y`、`.z`、`.w`（如果存在），以及颜色别名 `.r`、`.g`、`.b`、`.a`。
- 提供 `dot`、`cross`（仅三维和特殊二维）、`length`、`length_squared`、`normalize`、`lerp`、`reflect`、`refract`。
- 支持逐元素算术和与标量的乘除。
- 实现 `Zero`、`One` 等 trait（`Vec3::ONE` 为全 1 向量）。
- 提供 `angle_between` 和 `distance_to` 等几何方法。
- 可为任意维度实现 `Swizzle` 操作，但通过宏生成而非真正泛型，以保证编译时错误检查。

#### 3.2.2 矩阵（Matrix）

- 具体类型：`Mat2<T>`、`Mat3<T>`、`Mat4<T>`。
- 通用类型：`Matrix<T, Rows, Cols>`。
- 按列优先存储，与图形管线一致。
- 基本运算：加法、减法、标量乘法、矩阵乘法（`*` 运算符，维数不匹配时编译错误）。
- 转置（`transpose`）、逆（`inverse` 返回 `Option` 或 `Result`，因为可能奇异）、行列式（`determinant`）、迹（`trace`）、秩（`rank`
  ，仅小尺寸通过 Gaussian 消元）。
- 提供 `identity`、`from_diagonal`、`from_rows`、`from_columns` 等构造器。
- 对于 `Mat4<T>`，特别提供透视投影、正交投影、look-at 等视图变换构造方法（因与几何紧密相关，但不包含“相机”术语，仅用数学名：
  `perspective_projection`、`orthographic_projection`）。
- 与向量乘法：`matrix * vector` 产生向量，维数自动匹配。
- 分解（仅小尺寸）：LU、QR、Cholesky（对称正定）、特征值（仅 2x2、3x3 提供闭式解）。

#### 3.2.3 四元数（Quaternion）

```rust
struct Quaternion<T> {
    w: T,
    x: T,
    y: T,
    z: T,
}
```

- 用于三维旋转表示，避免万向节锁。
- 提供 `from_axis_angle`、`from_rotation_matrix`、`from_euler_angles`（旋转顺序可指定）。
- 实现 `slerp`（球面线性插值）、`lerp`（后归一化）。
- 实现 `conjugate`、`inverse`、`normalize`。
- 与 `Vec3` 的乘法实现旋转操作：`quaternion * vector`。
- 提供 `to_rotation_matrix` 转换。

### 3.3 Tensor（多维数组）

Tensor 子模块提供通用的多维数组抽象，是科学计算和数据处理的基础，但与 `Sonic.Data.DataFrame` 明确区分：Tensor
关注数值计算，无标签轴，无缺失值处理（缺失由 `Option` 或 NaN 表示）。

#### 3.3.1 核心结构

```rust
struct Tensor<Element, Dimension, Storage = DefaultStorage> {
    storage: Storage,
    shape: Dimension,
    strides: Dimension,
}
```

- `Element`：元素类型，通常为 `f32`、`f64`、`Complex` 等。
- `Dimension`：可以是动态的 `DynamicDimension`（运行时大小）或静态的 `StaticDimension<N>`（编译时大小）。
- `Storage`：底层存储 trait，默认为连续堆分配 `Vec<Element>`，也可为 `ViewStorage`（指向其它 Tensor 的一块内存）、
  `CudaStorage`（由外部绑定提供）等。
- 支持任意维数（0 维标量、1 维向量、2 维矩阵、高维张量）。

#### 3.3.2 操作

- **创建**：`zeros`、`ones`、`filled`、`from_slice`、`from_function`。
- **索引**：`tensor[[i, j, k]]` 返回元素引用；切片 `tensor.slice(axis, range)` 返回视图（不复制内存）。
- **变形**：`reshape`、`flatten`、`expand_dims`、`squeeze`。
- **广播**：通过 `Broadcast` 策略，使不同形状的张量在逐元素操作中自动对齐。
- **逐元素算术**：通过实现运算符 trait，支持 `+`、`-`、`*`、`/` 等，这些操作在形状兼容时自动广播。
- **归约**：`sum(axes)`、`mean`、`maximum`、`minimum`、`argmax`、`argmin`，可沿指定轴或全局。
- **线性代数**：`dot`（内积）、`tensordot`（广义缩并）、`matmul`（矩阵乘，即缩并最后两维）。
- **迭代**：`iter`、`iter_mut` 返回元素迭代器；`iter_axis` 沿轴迭代子张量。
- **并行**：如果标准库启用了并行，迭代器可配置为 `par_iter`，利用 `Sonic.Concurrency` 的数据并行能力。

#### 3.3.3 与外部后端集成

Storage trait 定义如下：

```rust
trait Storage<Element> {
    type Error;
    fn len(&self) -> usize;
    fn as_slice(&self) -> &[Element];
    fn as_mut_slice(&mut self) -> &mut [Element];
    fn allocate(shape: &[usize]) -> Result<Self, Self::Error>;
    // ... 其他必要方法
}
```

外部库可实现此 trait，将 Tensor 的数据驻留在 GPU、MKL 或分布式内存中。Sonic.Math 本身不提供任何非 CPU 的后端，但保证接口足以让下游集成。

### 3.4 Random（随机数）

Random 子模块提供可组合的随机数生成器 trait、多个内置引擎以及丰富的概率分布，且明确区分普通随机与密码学安全随机。

#### 3.4.1 核心 Trait

- `RandomNumberGenerator` trait：核心方法 `next_u32` 和 `next_u64`，并提供默认实现的 `fill_bytes`、`generate`（返回任何可随机生成类型）、
  `gen_range` 等。
- `SeedableRandomNumberGenerator` trait：继承 `RandomNumberGenerator`，并从种子创建。种子可为 `u64` 或固定大小字节数组。
- `CryptoRandomNumberGenerator` trait：一个标记 trait，表示该生成器适合密码学用途，具有不可预测性和后向安全性。

#### 3.4.2 内置引擎

- 非密码学：`XorShiftRng`、`WyRand`、`Pcg32`、`SplitMix64`。
- 密码学：`ChaCha12Rng`、`Hc128Rng`。这些均同时实现 `RandomNumberGenerator` 和 `CryptoRandomNumberGenerator`。
- `ThreadLocalRng`：一个自动创建的线程本地默认引擎，种子来自平台熵源（由 `Sonic.Platform` 提供）。

#### 3.4.3 分布

分布作为独立结构体，接受任意 `RandomNumberGenerator` 来产生样本。

- **均匀分布**：`Uniform`、`OpenClosed` 等。
- **正态分布**：`Normal`（使用 Box-Muller 或 Ziggurat）。
- **对数正态**：`LogNormal`。
- **指数分布**：`Exponential`。
- **伯努利与二项分布**：`Bernoulli`、`Binomial`。
- **泊松分布**：`Poisson`。
- **离散加权分布**：`WeightedIndex`，可用于抽样。
- 所有分布都提供 `sample` 方法，并可连续产生迭代器：`distribution.samples(rng)`。

#### 3.4.4 对种子安全的强制

`SeedableRandomNumberGenerator` 的 `from_entropy` 方法从平台获取安全熵，但该操作可能阻塞，文档须明确标注。

---

## 第四部分：Symbolic（符号计算）

Symbolic 子模块提供处理符号表达式的抽象，支持模式匹配、化简、微积分和逻辑表示，为计算机代数系统、编译器优化、定理证明等下游领域提供基础。

### 4.1 Expression（符号表达式与模式匹配）

#### 4.1.1 核心表达式树

表达式使用泛型枚举 `Expression<Value>`，其中 `Value` 可以是数字、符号或子表达式。

```rust
enum Expression<Value> {
    Symbol(Symbol),                // 来自 Sonic.Lang 的符号类型
    Constant(Value),               // 数值常量
    Application(Symbol, Vec<Expression<Value>>), // 函数应用，如 Sin[x]
    Add(Box<Expression>, Box<Expression>),
    Multiply(Box<Expression>, Box<Expression>),
    Power(Box<Expression>, Box<Expression>),
    // ... 其他基本函数节点
}
```

- 符号 `Symbol` 由 `Sonic.Lang` 提供，支持 interning 和快速相等比较。
- `Expression` 实现 `Display`，可输出标准数学表示法或 LaTeX（通过配置）。
- 提供 `evaluate(&self, environment: &HashMap<Symbol, Value>) -> Result<Value, EvaluationError>`，其中 `Value` 需实现相应的数值
  trait。

#### 4.1.2 模式与匹配

模式用于规则替换。模式是特殊表达式，可包含通配符。

- `Pattern` 枚举：`Wildcard(String)`、`PatternSequence(String)` 等。
- `MatchResult`：成功匹配时返回绑定列表。
- `matches(expression: &Expression, pattern: &Pattern) -> Option<HashMap<Symbol, Expression>>`。
- `replace_all(expression: &Expression, rules: &[(Pattern, Expression)]) -> Expression`：使用规则集进行重复替换直到不动点。

此模块提供基础的匹配和替换原语，但不包含完整的规则引擎或求值循环（属于下游框架职责）。

### 4.2 Calculus（符号微积分）

提供符号级别的微分、积分和级数展开。

- `differentiate(expression: &Expression, variable: Symbol) -> Expression`：应用链式法则返回导函数表达式。支持所有基本函数（
  `Sin`, `Cos`, `Exp`, `Log` 等）的导数。
- `integrate(expression: &Expression, variable: Symbol) -> Option<Expression>`：尝试不定积分，使用启发式 Risch 算法简化版。返回
  `None` 表示无法找到闭式解。
- `taylor_series(expression: &Expression, variable: Symbol, around: Value, order: usize) -> Expression`：生成截断幂级数。
- `limit`：为可选特性，仅提供基于洛必达的简单试探。

### 4.3 Polynomial（多项式与有理函数）

专门处理多项式、有理函数及其操作，因为它们具有良态代数。

- `Polynomial<Coefficient>`：存储为稀疏或稠密单项式列表。单项式为系数乘以变量幂的乘积。
- 支持：加法、乘法、长除法、求根（数值方法如 Durand-Kerner 或符号式因式分解）、最大公因式、因式分解（小多项式或有限域上）。
- `RationalFunction<Polynomial>`：分子和分母均为多项式，自动约分。
- 提供 `evaluate` 代入数值，`substitute` 符号替换。
- 与 `Expression` 可互转：`from_expression` 和 `to_expression`。

### 4.4 Logic（逻辑表达式）

逻辑子模块提供布尔表达式和量词抽象，用于约束求解和形式化验证。

- `BooleanExpression` 枚举：`True`、`False`、`Variable(Symbol)`、`Not`、`And`、`Or`、`Implication`、`Equivalent`。
- `Quantifier`：`ForAll(Vec<Symbol>, Box<BooleanExpression>)`、`Exists(...)`。
- 提供基本化简：德摩根律、吸收律、真值表求值。
- 提供 `satisfiability` 的接口：`is_satisfiable` 仅通过穷举或启发式（不集成复杂 SAT 求解器），但提供 trait
  `SatisfiabilitySolver`，下游可接入专业求解器。
- 可转换为合取范式（`conjunctive_normal_form`）。

---

## 第五部分：Geometry（几何抽象）

Geometry 提供了纯粹的数学几何原语，不含任何渲染或窗口概念。

### 5.1 Primitive（基本形体与网格抽象）

#### 5.1.1 点与向量

- `Point<Coord, N>` 和 `Vector<Coord, N>` 明确区分几何语义：点不可加，但可减得向量；点加向量得点。
- 提供 `distance`、`midpoint` 等操作。

#### 5.1.2 基础形状

- `LineSegment`：由两端点定义。
- `Ray`：原点和方向。
- `Plane`：法向量和偏移。
- `Triangle`：三个点。
- `Sphere`：球心和半径。
- `AxisAlignedBoundingBox`、`OrientedBoundingBox`：用于快速碰撞剔除。
- `Frustum`：视锥体，由六个平面定义。
- 所有形状均提供 `bounding_volume` 返回它们的包围体，便于加速结构。

#### 5.1.3 网格抽象

```rust
trait Mesh {
    type Vertex: PrimitivePoint;
    fn vertices(&self) -> &[Self::Vertex];
    fn indices(&self) -> Option<&[u32]>; // None 表示非索引几何
    fn face_count(&self) -> usize;
    fn foreach_face<F>(&self, callback: F) where F: FnMut(Face<Self::Vertex>);
}
```

此 trait 使得任何实现了网格接口的类型（无论来自文件加载还是程序化生成）都能直接参与几何计算，如碰撞检测、体积计算、点面最近距离等。

### 5.2 Transform（变换）

提供刚体、仿射和投影变换的数学表示，不关心图形管线状态。

- `Isometry2<T>`、`Isometry3<T>`：旋转（或反射）+ 平移，使用单位四元数或旋转矩阵。
- `Similarity2<T>`、`Similarity3<T>`：等距变换加统一缩放。
- `Affine2<T>`、`Affine3<T>`：通用 3x3 或 4x4 矩阵表示，允许非均匀缩放和切变。
- `PerspectiveProjection`、`OrthographicProjection`：投影变换矩阵构造器，不涉及视口映射。
- 所有变换实现 `compose`、`inverse`，并提供 `transform_point`、`transform_vector` 等方法。
- 支持 `interpolate`（例如 `Isometry::lerp` 和 `slerp`）。

### 5.3 Intersection（碰撞检测与距离）

提供成对几何体之间的相交测试和最近距离计算，所有函数返回统一的结果类型。

```rust
enum Intersection {
    Disjoint,
    Intersect { point: Point, normal: Vector },
    Overlap { depth: f64, direction: Vector }, // 用于穿透
}
```

具体函数：

- `ray_intersects_triangle`、`ray_intersects_sphere` 等。
- `aabb_intersects_aabb`、`sphere_intersects_sphere`。
- `closest_points_between_segment_and_segment` 等距离查询。
- `point_in_triangle`、`point_in_convex_polygon`。
- 支持 `Mesh` 与 `Ray` 的交点检测（返回第一个交点），但遍历面由用户或下游库控制，标准库只提供单面测试。

---

## 第六部分：Calculus（连续数学与数值分析）

此模块专注于连续数值方法，与 `Symbolic.Calculus` 区分：前者操作表达式，此处操作数字。

### 6.1 AutoDiff（自动微分）

提供前向和反向自动微分的抽象与参考实现，不绑定任何计算图库。

#### 6.1.1 前向模式

- `ForwardDual<T, Tangent>` 结构：携带值和对偶部分。
- 实现所有数值 trait，使得编写可微函数时透明。
- 提供 `derivative` 和 `jacobian` 辅助函数，自动遍历函数并计算导数。

#### 6.1.2 反向模式

- 通过 `Recordable` trait 实现操作重载，建立计算图（`WengertList`）。
- `Variable<T>` 包装类型，每次操作隐式记录。
- `gradient` 函数：给定标量输出，反向传播计算所有叶子变量的梯度。
- 标准库提供基本的 `WengertList` 实现，但核心 trait `GradientTape` 允许下游库提供自定义实现（例如支持 GPU 操作的磁带）。

#### 6.1.3 可微分 Trait

```rust
trait Differentiable {
    type Tangent;
    fn forward_differential(&self, seed: &Self::Tangent) -> (Self, Self::Tangent);
    // ...
}
```

任何类型只要实现了 `Differentiable`，就可以参与自动微分。

### 6.2 Integration（数值积分与微分方程）

- **数值积分**：
  - `trapezoidal_rule`、`simpsons_rule`、`gauss_legendre`（一维）。
  - 多维积分通过迭代一维积分实现。
  - 接受函数指针或闭包作为被积函数。
- **微分方程初值问题**：
  - 提供显式 Runge-Kutta 方法（RK4、Dormand-Prince 5 (4) 自适应步长）。
  -
  `solve_ivp(system: &dyn DynamicalSystem, initial_state: &Vector, time_span: (f64, f64), step_size: f64) -> Vec<(f64, Vector)>`。
  - `DynamicalSystem` trait 需要用户实现 `fn derivative(&self, state: &Vector) -> Vector`。
  - 不提供刚性问题求解器（如隐式方法），但 trait 设计允许下游扩展。

### 6.3 Optimization（数值优化器骨架）

提供迭代优化算法的抽象框架，依赖于 `AutoDiff` 或用户提供的梯度。

- `OptimizationProblem` trait：定义 `evaluate`（目标函数）和 `gradient`（梯度，可选）。
- `Optimizer` trait：定义 `step` 方法，接受问题和当前点，返回新点。
- 内置参考实现：
  - `GradientDescent`：学习率参数。
  - `Adam`、`RMSprop`：自适应学习率。
  - `LBFGS` 的简单版本（仅存储有限内存向量序列）。
- 不包含约束优化（如内点法），但 trait 设计上留出约束处理的扩展点（如 `ConstrainedOptimizationProblem`）。

---

## 第七部分：Combinatorics（组合数学）

### 7.1 Permutation（排列与置换）

- `Permutation` 结构：存储从 0 到 n-1 的排列映射。
- 提供 `identity`、`random`、`inverse`、`compose`。
- 迭代生成：`all_permutations(n)` 返回惰性迭代器，按字典序生成。
- `next_permutation`、`previous_permutation` 就地改变切片。
- `rank`（排列的字典序排名）和 `unrank`。
- 置换的奇偶性 `is_even`。

### 7.2 Combination（组合与子集）

- `combinations(sequence, k)` 返回所有 k 元子集的迭代器。
- `combinations_with_replacement` 同理。
- `binomial(n, k)` 计算二项式系数，结果为 `BigInteger` 以避免溢出；另提供 `binomial_mod` 用于模运算。
- 子集生成：`subsets(sequence)` 返回所有子集的迭代器。

### 7.3 Partition（整数分拆与集合分拆）

- `partitions(n: u64) -> impl Iterator<Item = Vec<u64>>`：整数分拆，每个分拆为降序序列。
- `set_partitions<T: Clone>(set: &[T]) -> impl Iterator<Item = Vec<Vec<T>>>`：集合分拆。
- `stirling2(n, k)`：第二类斯特林数。
- `bell_number(n)`。

### 7.4 Generating（生成函数与数列）

- `Catalan`、`Derangement`、`Eulerian`、`Harmonic` 数列通过常量或函数提供。
- `FormalPowerSeries<Coefficient>`：以生成函数的抽象形式存储系数序列（有限或无限惰性）。
- 支持序列加法、乘法（卷积）、复合（有限情况），但复杂度警告。

---

## 第八部分：GraphTheory（图论）

### 8.1 Graph（图数据结构）

- `Graph<NodeWeight, EdgeWeight, Directed, Storage>` 泛型结构。
  - `Directed` 是编译时布尔标记（`Directed` 或 `Undirected`）。
  - `Storage` 默认为邻接表（`AdjacencyList`），也可选择邻接矩阵、CSR 等。
- `NodeIndex`、`EdgeIndex` 为不透明类型，保证安全。
- `GraphBase` trait：提供 `add_node`、`remove_node`、`add_edge`、`remove_edge`、`node_count`、`edge_count` 等基本操作。
- `AdjacencyAccess` trait：提供 `neighbors(node: NodeIndex) -> &[NodeIndex]` 等。
- 提供与 `DataFrame` 的互转方法（`to_edge_dataframe`、`from_edge_dataframe`），方便序列化与分析。

### 8.2 Traversal（遍历）

- `BreadthFirstSearch` 结构：实现迭代器 `Iterator<Item = (NodeIndex, usize)>`（节点及深度）。
- `DepthFirstSearch` 结构：迭代器。
- `topological_sort`：返回 `Vec<NodeIndex>` 或 `Err`（检测到环）。
- 连通分量：`connected_components`（无向图）、`strongly_connected_components`（有向图，使用 Tarjan 或 Kosaraju 算法）。
- `has_cycle` 检测。

### 8.3 Algorithm（经典算法）

所有算法实现为独立函数，接受实现了相应 trait 的图参数。

- **最短路径**：
  - `dijkstra`（非负权重）、`bellman_ford`（允许负权，检测负环）。
  - `floyd_warshall`（全源）。
  - 返回 `HashMap<NodeIndex, (Weight, Predecessor)>`。
- **最小生成树**：
  - `kruskal` 返回 `Vec<EdgeIndex>`。
  - `prim` 返回 `Vec<EdgeIndex>`。
- **流网络**：
  - `edmonds_karp` 计算最大流，返回流量值和残差图。
  - `MaxFlow` trait 可让用户替换为 Push-Relabel 等其他算法。
- **匹配**：`hopcroft_karp` 求二分图最大匹配。
- **欧拉与哈密顿**：`has_eulerian_path`、`has_eulerian_circuit`；哈密顿检测仅提供回溯搜索（NP 完全，但图小时可用）。

### 8.4 Metric（图度量）

- `degree_centrality`、`betweenness_centrality`、`closeness_centrality`（均为标准定义，复杂度可能较高，文档注明）。
- `density`、`radius`、`diameter`（基于全对最短路径的近似，大图警告）。
- `clustering_coefficient`。

---

## 第九部分：NumberTheory（数论）

### 9.1 Prime（素数测试与筛法）

- `is_prime(n: &BigInteger) -> bool`：确定性 Miller-Rabin 对小整数，对任意大整数采用概率测试结合 Lucas 测试提供确定性（对于
  64 位输入可完全确定）。
- `next_prime`、`previous_prime`。
- `sieve_of_eratosthenes(limit: usize) -> Vec<u64>`：生成直到 limit 的所有素数。
- `prime_factors(n: &BigInteger) -> Vec<BigInteger>`：返回素因子（多重）。

### 9.2 Modular（模运算）

- `modular_power(base, exponent, modulus)`。
- `modular_inverse(value, modulus) -> Option<BigInteger>`。
- `chinese_remainder_theorem(residues: &[(BigInteger, BigInteger)]) -> Option<BigInteger>`：输入余数和模，返回满足同余的解。
- `legendre_symbol`、`jacobi_symbol`。
- `sqrt_mod`（模素数平方根，Tonelli-Shanks 算法）。

### 9.3 CryptoTool（密码学数论工具）

- `discrete_logarithm(base, result, modulus)`：使用 Baby-step Giant-step 或 Pollard's rho，标记为耗时操作。
- `quadratic_sieve_factor`：大整数因式分解（作为参考实现，但注明性能限制）。
- 为密码学提供支撑，但本身不实现任何加密协议。

---

## 第十部分：Crypto（密码学原语）

密码学模块是 `Sonic.Math` 的应用数学分支，提供经过安全审计的密码原语。所有实现均保证常量时间（对于分支敏感操作），文档标注安全假设。

### 10.1 Hash（哈希函数）

- `Hasher` trait：`update(&mut self, data: &[u8])`、`finalize(self) -> Vec<u8>`、`reset`。
- 实现：
  - `Sha256Hasher`、`Sha512Hasher`。
  - `Sha3_256Hasher`、`Sha3_512Hasher`。
  - `Blake3Hasher`：支持并行和密钥化。
- 提供便利函数 `sha256(data: &[u8]) -> [u8; 32]` 等。

### 10.2 Aead（认证加密）

- `AeadCipher` trait：
  - `encrypt(key: &[u8], nonce: &[u8], associated_data: &[u8], plaintext: &[u8]) -> Result<Vec<u8>, CryptoError>`。
  - `decrypt(...) -> Result<Vec<u8>, CryptoError>`。
- 实现：
  - `Aes128Gcm`、`Aes256Gcm`（使用硬件加速如 AES-NI 通过条件编译，但标准库只提供软件实现，高性能加速由外部绑定实现）。
  - `ChaCha20Poly1305`。
- 密钥长度和 nonce 长度在类型级别约束（通过 const generics）。

### 10.3 Signature（数字签名）

- `SignatureScheme` trait：
  - `key_pair() -> (PrivateKey, PublicKey)`。
  - `sign(private_key: &PrivateKey, message: &[u8]) -> Signature`。
  - `verify(public_key: &PublicKey, message: &[u8], signature: &Signature) -> bool`。
- 实现：
  - `Ed25519`（基于 Curve25519）。
  - `EcdsaP256`、`EcdsaP384`（使用 NIST 曲线，实现需注意侧信道，仅提供基本实现）。
- 密钥类型封装，支持导入/导出（PKCS#8 格式），但不负责证书链验证。

### 10.4 Kem（密钥封装）

- `KeyEncapsulationMechanism` trait：
  - `keygen() -> (PublicKey, PrivateKey)`。
  - `encapsulate(public_key: &PublicKey) -> (Ciphertext, SharedSecret)`。
  - `decapsulate(private_key: &PrivateKey, ciphertext: &Ciphertext) -> SharedSecret`。
- 实现：`X25519`（与 `Hkdf` 组合生成共享密钥）、`EcdhP256` 等。

### 10.5 Kdf（密钥派生）

- `KeyDerivationFunction` trait：`derive(password: &[u8], salt: &[u8], length: usize) -> Vec<u8>`。
- 实现：
  - `Hkdf`（基于 HMAC 的提取-扩展）。
  - `Pbkdf2`（可配置迭代次数）。
  - `Argon2id`（内存困难，提供参考实现，参数可调）。

---

## 第十一部分：Constants（数学常量）

提供高精度、无缩写的数学常量，均为 `const` 或 `static`。

- `PI`、`TAU`（2π）、`FRAC_PI_2`、`FRAC_PI_3`、`FRAC_PI_4`。
- `E`（自然常数）、`LOG2_E`、`LOG10_E`、`LN_2`、`LN_10`。
- `SQRT_2`、`FRAC_1_SQRT_2`。
- `GOLDEN_RATIO`（φ）。
- 所有常量提供 `f32` 和 `f64` 精度，对于更高精度需求，提供 `BigDecimal` 版本（懒加载）。

---

## 第十二部分：跨模块协作与边界

### 12.1 与 Sonic.Lang 的集成

- 所有数学类型均派生或实现 `Debug`、`Display`、`Clone`、`PartialEq`、`Eq`、`PartialOrd`、`Ord`（若可能）、`Hash`。
- `Symbol` 类型由 `Lang` 提供，`Symbolic` 使用它，保证符号的比较效率。
- `Lang` 的宏系统可用于生成数学类型的 boilerplate（如 `derive(Additive, Multiplicative)` 等）。

### 12.2 与 Sonic.Data 的集成

- `Tensor` 可作为 `DataFrame` 的列存储后端，通过视图实现零拷贝。
- `GraphTheory.Graph` 可转化为 `DataFrame`（边表）进行序列化。
- `Data` 的序列化框架 `Serialize`/`Deserialize` trait 为所有数学类型实现，支持 JSON、MessagePack 等格式。

### 12.3 与 Sonic.Platform 的集成

- 随机数熵源通过 `Platform` 的 `entropy_source()` 函数获取，返回 `[u8; 32]` 等。
- `Time`（`Instant`、`Duration`）作为参数出现在数值积分、基准测试中，但 `Math` 本身不直接提供时间，只接受这些类型作为输入。

### 12.4 与 Sonic.Concurrency 的集成

- `Tensor` 的并行迭代器和图算法的并行版本（如并行 BFS）可利用 `Concurrency` 提供的工作窃取调度器或数据并行抽象。

---

## 第十三部分：绝对禁止的边界

为确保 `Sonic.Math` 保持纯净的数学抽象，以下内容被严格排除：

1. **不实现任何行业领域模型**：无 Black-Scholes 定价公式、无粒子系统、无机器人正运动学链、无神经层（如 `LinearLayer`、
   `Convolution2D`）。
2. **不包含特定硬件加速代码**：无 CUDA kernel、无 Metal shader、无 MKL 调用。所有加速由外部通过 trait 后门（如 `Storage`
   trait）集成。
3. **不提供完整的统计或机器学习框架**：无 `DataFrame` 上的线性回归 `fit` 方法，无 `predict`，无训练循环。
4. **不实现完整的符号代数系统**：无基于规则的自动推理引擎、无 `Solve` 命令（仅多项式求根基础）、无 `Simplify` 的完整启发式。
5. **不绑定图形渲染**：无顶点缓冲区、无着色器、无纹理、无窗口。
6. **不实现网络协议**：`Crypto` 给出密钥和签名，但不做 TLS 握手。
7. **不使用缩写或非数学术语**：一切命名保持完整性和数学纯度。

---

## 第十四部分：最终总结

`Sonic.Math` 经过精心设计，覆盖了从基础运算、数值计算、符号操作到几何、图论、组合学、数论和密码学的全部数学范畴。每个子模块都提供了清晰的
trait 抽象、高性能的默认实现以及严格的边界，确保其既可以作为科学计算、数据分析、图形渲染、密码学等领域的基础，又不会因为承载过多具体应用逻辑而变得臃肿或不安全。它践行了“标准库只做抽象与基础实现，下游库自由组合扩展”的设计哲学，是
Sonic 全能标准库不可或缺且最为纯粹的部分。

## 1.1 补充案：算法解耦与下游极限优化设计

### 一、核心原则

Sonic.Math 在提供数学基础设施时，严格区分 **抽象定义**与 **具体算法实现**。标准库的角色是定义可组合的细粒度
trait，并给出保证正确性但未必最高性能的默认算法，而绝不通过全局后端或大容器来固化任何计算路径。这一设计确保下游领域库或最终应用可以在不修改标准库的情况下，对任意环节进行极限优化，甚至完全替换算法而依然与生态中其他组件无缝协作。

### 二、算法独立性与细粒度 Trait 绑定

所有算法均作为独立函数或类型提供，其泛型参数仅约束为实际所需的 trait，绝不要求实现某个大一统的 `MathematicsEngine` 或
`LinearAlgebraBackend`。例如：

- 最短路径算法 `shortest_path_dijkstra` 并不要求图类型实现一个笼统的 `GraphBackend`，而是只依赖于 `GraphBase`
  （提供节点和边的基本访问）、`EdgeWeight`（权重类型可加、可比较）以及 `IndexMap`（存储距离和前驱）。任何自定义图存储只要满足这三个
  trait，就可直接调用该算法。
- 数值积分函数 `integrate_simpson` 仅依赖被积函数 `Fn(f64) -> f64`，不绑定任何函数源或表达式系统。
- 矩阵乘法 `matrix_multiply` 仅要求左右操作数实现 `MatrixView` trait（提供行数、列数、元素访问），结果写入实现 `MatrixMut`
  的目标。标准库提供一个基于三重循环的朴素实现，但用户可为特定存储（如分块内存、GPU 缓冲区）提供特化版本，且特化版本仅需覆盖矩阵乘法这一个函数，不需动其它任何组件。

这种设计消除了“为了实现一个算法的优化，必须实现整个后端”的沉重负担。

### 三、默认实现作为正确性基准，而非性能天花板

Sonic.Math 中每一个算法都会提供一个在通用 CPU 上足够简单、易于审计的参考实现，并明确在文档中标注其复杂度与适用场景。这些参考实现的目的有三：

1. **行为标准**：所有针对同一算法的优化版本必须产生与参考实现一致的结果（在合理数值误差范围内）。
2. **即时可用**：使用者无需任何外部依赖即可获得基本能力，快速验证逻辑。
3. **替换锚点**：当需要更高性能时，开发者清楚知道该替换哪一个函数，并且可以用参考实现作为测试的 oracle。

例如，`sort_by_key` 在标准库中基于模式消除的快速排序，但允许通过特化或链接时优化替换为 Radix Sort 或并行排序。图的最短路径算法，标准库提供
Dijkstra（二叉堆），文档明确指出对于大规模图或特定图结构，下游可替换为 A*、使用 Fibonacci 堆的 Dijkstra、Δ-stepping 或 GPU
并行版本，替换只需实现同一个函数签名的另一个版本并调整调用。

### 四、避免大容器：不存在全局 Backend Trait

在设计上，Sonic.Math 绝不定义类似以下的大统一 trait：

```rust
// 标准库中永远不会出现此类容器
trait MathBackend {
    type Vector;
    type Matrix;
    fn matrix_multiply(a: &Self::Matrix, b: &Self::Matrix) -> Self::Matrix;
    fn solve_linear_system(a: &Self::Matrix, b: &Self::Vector) -> Self::Vector;
    fn eigen_decomposition(a: &Self::Matrix) -> (Self::Matrix, Self::Vector);
    // ... 几十个方法
}
```

原因有三：

- **耦合过重**：实现者不得不一次性处理所有算法，哪怕只需要优化矩阵乘法。
- **抽象泄漏**：大 trait 通常隐含某种数据布局假设（例如稠密列优先），限制了稀疏、分块或分布式存储的融合。
- **扩展性差**：新增一种算法需要修改全局 trait，违反开闭原则。

替代方案是，每个算法或每组紧密相关的算法，定义自己所需的最小接口。例如，线性方程求解函数 `solve_linear_system` 仅依赖
`MatrixView` 和 `VectorView`，与矩阵乘法无关；特征值分解可能需要额外的 `EigenDecomposable` trait。用户可以逐步为其数据结构实现所需
trait，渐进式接入标准库算法。

### 五、下游极限优化的典型路径

假设一个科学计算框架需要在 GPU 上加速线性代数。它无需依赖标准库提供任何 GPU 代码，而是可以：

1. 定义自己的 `GpuMatrix<T>` 和 `GpuVector<T>`。
2. 为它们实现 `MatrixView`、`MatrixMut`、`VectorView` 等标准库定义的基本访问 trait（通过 FFI 调用 CUDA
   内核获取元素，但为避免频繁跨设备传输，实际实现可能会更复杂，不过这属于框架内部优化）。
3. 直接替换 `matrix_multiply`，提供 `matrix_multiply_gpu` 版本，内部调用 cuBLAS。该框架只需在调用处用此函数替代标准库默认实现。
4. 如果框架还想使用标准库的迭代求解器（例如 `conjugate_gradient`），该求解器只依赖矩阵-向量积（由 `MatrixView` 和
   `VectorView` 上的操作自动提供，或特化一个 `matrix_vector_product` 函数），因此 GPU 矩阵可直接作为参数传入。开发者无需重新实现整个求解器。

另一个例子：游戏物理引擎需要极快的碰撞检测。标准库提供基于通用 `Mesh` trait 的 `triangle_mesh_intersection`
函数，使用遍历所有面片的朴素方法。引擎可以为其自定义网格类型实现 `Mesh` trait，然后特化 `triangle_mesh_intersection` 为自己的
BVH 加速版本。原有函数签名不变，引擎其他部分无感知。

### 六、允许替换的最小单元

替换粒度细至单个函数，而非模块。标准库通过以下机制支持这一点：

- **孤儿规则宽松**：由于标准库和下游类型都是同一语言生态的一等公民，可以为外部类型实现外部 trait（在允许的情况下），或通过新类型包装提供特化实现。
- **特征泛型与特化**：算法函数是泛型的，如果某类型提供了更高效的内在方法（例如 `Gemv`
  trait），算法会自动调度到特化版本；否则回退到基于基本访问的通用实现。这通过 trait 的默认实现和方法覆盖完成。
- **特征与算法分离**：算法本身不定义为 trait 的方法，而是独立函数，这样用户就不必为了重写算法而实现一个庞大的
  trait。例如，排序算法是独立函数 `sort`，而不是 `Sortable` trait 的方法，因此不同容器可以提供完全不同的排序实现，甚至可以引入外部排序算法库。

### 七、与外部加速库的整合

标准库不在任何地方假定数据驻留在 CPU 内存。`Storage` trait（用于 `Tensor`）和 `MatrixView` 等访问 trait
均通过关联函数返回引用或切片，但不暴露底层指针。下游可通过 FFI 绑定 MKL、cuBLAS、oneDNN 等，然后将调用封装为对标准库算法的直接替换或通过
trait 特化注入。标准库本身只提供纯计算逻辑，不关心具体指令集，但通过 `std::simd` 等可移植 SIMD
抽象，为默认实现提供一定程度的自动向量化能力，且不锁定平台。

### 八、对组合与并行的支持

算法函数显式接受迭代器或分区参数，以便与 `Concurrency` 范畴协作。例如，`quicksort` 可接受一个指定并行阈值的配置，内部使用任务窃取调度器；但这由独立函数
`parallel_sort` 提供，而非将并行内置到核心算法中。同样，图算法提供顺序和可选的并行辅助函数（如
`parallel_connected_components`），依赖图存储能够安全并发访问。标准库不强制任何并发模型，只提供可组合的工具。

### 九、稳定性与版本管理

默认实现保证算法的数值稳定性（例如使用 Kahan
求和、主元选取），并承诺在相同输入下跨版本产生位级一致的结果（对于确定性算法）。下游优化版本可自行决定是否放宽稳定性以换取性能，但必须在文档中明确。标准库的
trait 定义尽可能稳定，具体函数实现可能随版本改进性能，但不会改变返回类型或语义。

### 十、补充结语

综上，Sonic.Math
的算法体系不是一块铁板，而是一盒可替换的积木。每一个数学运算都被拆解为最小依赖的抽象，配合可独立替换的参考实现。下游开发者无论是追求极致的单核性能，还是探索异构计算、分布式求解，都能在标准库的接口边界上自由发挥，而标准库本身则始终保持轻量、纯净和可移植。这正是“全能标准库”在数学范畴的核心承诺：提供一切必要的基础，但绝不成为创新的桎梏。