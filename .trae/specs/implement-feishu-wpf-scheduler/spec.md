# Feishu.Context WPF 调度平台 Spec

## Why
当前需要统一管理飞书会话信息采集和自定义 REST API 调度，通过 WPF 桌面应用实现飞书网页持久化登录、会话信息抓取与本地汇总，以及通过 REST API 获取查询和汇总信息。

## What Changes
- 创建 WPF 项目基础框架（.NET 8 + WPF）
- 集成 WebView2 实现飞书网页持久化登录与会话信息采集
- 实现本地数据持久化（SQLite）存储采集的会话信息和汇总表
- 实现 REST API 客户端模块，支持配置化调度接口
- 构建主界面，包含飞书会话采集视图和 API 调度视图

## Impact
- Affected code: 全新项目，无现有代码影响
- 依赖项: WebView2 SDK、SQLite、HttpClient、CommunityToolkit.Mvvm

## ADDED Requirements

### Requirement: WPF 项目基础框架
系统 SHALL 基于 .NET 8 创建 WPF 项目，采用 MVVM 架构模式，使用 CommunityToolkit.Mvvm 作为 MVVM 框架。

#### Scenario: 项目结构创建
- **WHEN** 项目初始化完成
- **THEN** 项目包含 Models、ViewModels、Views、Services、Data 等目录，遵循 MVVM 分层

### Requirement: 飞书网页持久化登录
系统 SHALL 在 WPF 中嵌入 WebView2 控件，持久化飞书网页登录状态，支持用户手动登录后保持会话。

#### Scenario: 首次登录
- **WHEN** 用户首次打开飞书页面
- **THEN** 显示飞书登录页面，用户完成登录后会话状态被持久化到本地 WebView2 用户数据目录

#### Scenario: 后续访问
- **WHEN** 用户再次打开应用
- **THEN** WebView2 自动恢复之前的登录状态，无需重新登录

### Requirement: 飞书会话信息采集
系统 SHALL 支持用户指定飞书会话，自动或手动采集会话中的特征信息和内容，并记录到本地数据库。

#### Scenario: 指定会话采集
- **WHEN** 用户选择或输入目标会话标识
- **THEN** 系统通过 WebView2 注入脚本抓取该会话的消息内容

#### Scenario: 信息记录
- **WHEN** 采集到会话信息
- **THEN** 系统将消息内容、发送者、时间等特征信息存储到本地 SQLite 数据库

#### Scenario: 汇总展示
- **WHEN** 用户查看采集记录
- **THEN** 系统以表格形式展示汇总的会话信息列表，支持按时间、会话等维度筛选

### Requirement: REST API 调度
系统 SHALL 提供 REST API 客户端模块，支持配置化的 API 接口调度，获取查询信息和汇总信息。

#### Scenario: API 配置
- **WHEN** 用户配置 API 端点（URL、方法、请求头、请求体模板）
- **THEN** 系统保存配置并可在调度时使用

#### Scenario: 手动触发 API 调用
- **WHEN** 用户点击触发按钮
- **THEN** 系统执行 API 请求并展示返回结果

#### Scenario: 定时调度
- **WHEN** 用户配置定时调度规则
- **THEN** 系统按规则自动执行 API 调用，记录每次调用的结果

#### Scenario: 结果汇总
- **WHEN** API 调用返回结果
- **THEN** 系统将结果存储到本地数据库，并在界面上展示汇总信息

### Requirement: 本地数据持久化
系统 SHALL 使用 SQLite 作为本地数据库，存储飞书会话采集数据和 API 调度结果。

#### Scenario: 数据库初始化
- **WHEN** 应用首次启动
- **THEN** 自动创建 SQLite 数据库及所需表结构（会话信息表、API 调度记录表、API 配置表）

#### Scenario: 数据查询
- **WHEN** 用户查询历史数据
- **THEN** 系统从 SQLite 中检索并返回结果
