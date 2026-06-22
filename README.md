# Seiun（星云）— 外语智能学习平台

Seiun 是一个基于 .NET 9 构建的外语智能学习后端服务，结合 AI 能力为用户提供个性化的单词学习、阅读训练和题目批改体验。

## 技术栈

| 类别 | 技术 |
|---|---|
| 框架 | .NET 9 (ASP.NET Core Web API) |
| 数据库 | PostgreSQL + Entity Framework Core |
| 对象存储 | MinIO |
| 认证 | JWT (Bearer Token) |
| 搜索引擎 | Elasticsearch (可选，已注释) |
| AI 服务 | DeepSeek / OpenAI 兼容 API / DALL·E |
| 文档 | Swagger (Swashbuckle) |
| 图片处理 | SixLabors.ImageSharp |

## 核心功能

### 🧠 AI 智能学习
- **AI 生成阅读文章**：根据用户学习进度，自动生成适合当前水平的阅读材料
- **AI 生成练习题目**：自动生成完形填空（Cloze）和选词填空（Fill-in-blank）题目
- **题目批改**：上传作业图片，通过 AI 进行智能批改，结果通过 SSE 实时流式返回
- **单词提取**：从文本内容中自动提取生词

### 📚 单词学习
- **学习会话（Session）**：开始单词学习会话，支持中断续学
- **单词书（WordBook）**：自定义单词本，组织和管理词汇
- **干扰项（Distractor）**：为选择题自动生成迷惑选项
- **已完成/错误单词追踪**：记录已掌握单词和常错单词

### 📝 社区互动
- **文章发布**：创作者可发布外语学习文章
- **点赞与评论**：支持文章点赞、评论和多级回复
- **AI 文章**：AI 自动生成的阅读文章

### 📊 学习管理
- **学习计划**：设置每日学习目标
- **每日打卡**：连续打卡天数追踪
- **错题本（MistakeBook）**：记录错题，便于复习

### 👤 用户系统
- 手机号 / 用户名 / 邮箱登录
- JWT Token 认证与续签
- 个人资料编辑与头像上传（自动裁剪转 WebP）
- 多角色权限：User / Creator / Admin / SuperAdmin

## 项目结构

```
Seiun/
├── Controllers/       # API 控制器（13 个）
│   ├── AdminController        # 管理员接口
│   ├── ArticleController      # 文章接口
│   ├── ChallengeController    # 题目接口
│   ├── CommentController      # 评论接口
│   ├── MistakeBookController  # 错题本接口
│   ├── ReplyController        # 回复接口
│   ├── ResourceController     # 资源接口
│   ├── TestAnalysisController # AI 批改接口
│   ├── UserController         # 用户接口
│   ├── UserPlanController     # 学习计划接口
│   ├── WordBookController     # 单词书接口
│   └── WordSessionController  # 学习会话接口
├── Entities/          # 数据库实体与 DbContext
├── Models/
│   ├── Parameters/    # 请求 DTO
│   └── Responses/     # 响应 DTO
├── Repositories/      # 数据访问层
├── Services/          # 业务逻辑层
├── Filters/           # 请求过滤器（参数校验）
├── Utils/             # 工具类、枚举、常量
├── Resources/         # 消息文案（中英文）
└── Program.cs         # 应用入口与 DI 配置
```

## 快速开始

### 环境要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL 数据库
- MinIO 对象存储（可选，用于头像和图片上传）

### 配置

编辑 `appsettings.json`（或 `appsettings.Development.json`）：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=seiun;Username=your_user;Password=your_password"
  },
  "Jwt": {
    "Secret": "your-jwt-secret-key",
    "Issuer": "MisakaNetwork",
    "Audience": "SeiunUser"
  },
  "MinIO": {
    "Endpoint": "127.0.0.1:9000",
    "AccessKey": "your-minio-access-key",
    "SecretKey": "your-minio-secret-key"
  },
  "GenerateAiArticle": {
    "ArticleEndpoint": "https://api.deepseek.com",
    "ArticleModel": "deepseek-chat"
  }
}
```

### 运行

```bash
# 安装依赖
dotnet restore

# 启动开发服务器
dotnet run

# 访问 Swagger 文档
# https://localhost:5001/swagger
```

### 数据库迁移

```bash
# 创建迁移
dotnet ef migrations add InitialCreate

# 应用迁移
dotnet ef database update
```

## API 概览

| 路由 | 说明 |
|---|---|
| `POST /api/user/register` | 用户注册 |
| `POST /api/user/login` | 用户登录 |
| `GET /api/user/profile/{userId}` | 获取用户信息 |
| `PATCH /api/user/update-profile` | 更新个人信息 |
| `POST /api/user/upload-avatar` | 上传头像 |
| `GET /api/user/checkin` | 获取今日打卡状态 |
| `POST /api/session/init` | 开始单词学习 |
| `POST /api/session/check` | 检查单词答案 |
| `POST /api/challenge/list` | 获取题目列表 |
| `POST /api/test-analysis/correct-assignment` | AI 批改作业（SSE） |
| `POST /api/article/create` | 发布文章 |
| `GET /api/article/list` | 文章列表 |
| `POST /api/comment/create` | 发表评论 |

完整 API 文档请启动服务后访问 `/swagger` 查看。

## AI 服务配置

项目支持多个 AI 端点，可按需配置：

- **文章生成**：使用 DeepSeek Chat 模型
- **完形填空 / 选词填空生成**：使用 OpenAI 兼容 API（可配 o3-mini 等模型）
- **作业批改**：使用 o4-mini 模型
- **单词提取**：使用 gpt-4.1-nano 模型

各 AI 服务的 Endpoint 和 Model 可在 `appsettings.json` 中独立配置。

## 许可

内部项目，仅供学习与开发使用。
