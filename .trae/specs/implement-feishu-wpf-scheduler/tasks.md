# Tasks

- [ ] Task 1: 创建 WPF 项目基础框架
  - [ ] SubTask 1.1: 使用 dotnet CLI 创建 .NET 8 WPF 项目
  - [ ] SubTask 1.2: 添加 NuGet 依赖（Microsoft.Web.WebView2、Microsoft.Data.Sqlite、CommunityToolkit.Mvvm）
  - [ ] SubTask 1.3: 建立 MVVM 目录结构（Models、ViewModels、Views、Services、Data）
  - [ ] SubTask 1.4: 配置依赖注入（Microsoft.Extensions.DependencyInjection）
  - [ ] SubTask 1.5: 创建 MainWindow 主界面框架（含导航 TabControl）

- [ ] Task 2: 实现本地数据持久化层
  - [ ] SubTask 2.1: 创建 SQLite 数据库服务（DbService），包含初始化和表创建逻辑
  - [ ] SubTask 2.2: 定义数据模型（FeishuMessage、ApiConfig、ApiScheduleRecord）
  - [ ] SubTask 2.3: 实现会话信息表的 CRUD 操作
  - [ ] SubTask 2.4: 实现 API 配置表和调度记录表的 CRUD 操作

- [ ] Task 3: 实现飞书网页持久化登录与会话采集
  - [ ] SubTask 3.1: 创建 WebView2 宿主控件，配置用户数据目录实现登录持久化
  - [ ] SubTask 3.2: 实现飞书页面导航和登录状态检测
  - [ ] SubTask 3.3: 编写 JavaScript 注入脚本，抓取指定会话的消息内容
  - [ ] SubTask 3.4: 实现采集数据到本地数据库的存储逻辑
  - [ ] SubTask 3.5: 创建飞书采集视图（FeishuView）和 ViewModel，包含会话选择、采集触发、记录展示

- [ ] Task 4: 实现 REST API 调度模块
  - [ ] SubTask 4.1: 创建 HttpClient 服务封装（ApiService），支持 GET/POST 请求
  - [ ] SubTask 4.2: 实现 API 配置管理（增删改查 API 端点配置）
  - [ ] SubTask 4.3: 实现手动触发 API 调用和结果展示
  - [ ] SubTask 4.4: 实现定时调度（基于 DispatcherTimer），按配置规则自动执行
  - [ ] SubTask 4.5: 创建 API 调度视图（ApiView）和 ViewModel，包含配置管理、调度触发、结果展示

- [ ] Task 5: 集成与主界面完善
  - [ ] SubTask 5.1: 在 MainWindow 中集成飞书采集视图和 API 调度视图的 Tab 切换
  - [ ] SubTask 5.2: 实现应用启动时的数据库初始化和 WebView2 环境准备
  - [ ] SubTask 5.3: 添加应用配置文件（appsettings.json）管理基础配置

# Task Dependencies
- [Task 2] depends on [Task 1]
- [Task 3] depends on [Task 1, Task 2]
- [Task 4] depends on [Task 1, Task 2]
- [Task 5] depends on [Task 3, Task 4]
- [Task 3] 和 [Task 4] 可并行执行
