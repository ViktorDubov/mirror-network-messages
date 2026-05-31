# Mirror Subscription Service

Реализация сервиса отправки сетевых сообщений между сервером и клиентом на Unity + Mirror с фильтрацией по подпискам.

## Проблема

Стандартный механизм Mirror отправляет сообщения всем клиентам. Если клиент не зарегистрировал обработчик для типа сообщения — сервер его отключает (кидает исключение). Это неудобно при работе с большим количеством типов сообщений и не дает гибкости в выборе, что именно получать.

## Решение

Собственный сервис подписок поверх стандартных NetworkMessage Mirror. Клиент явно подписывается на нужные типы сообщений, сервер ведет реестр подписок и отправляет только тем, кто подписан. При этом обработчики регистрируются заранее, поэтому Mirror не отключает клиента.

## Структура

Assets/
├── Scripts/

│   ├── Shared/

│   │   ├── GameLifetimeScope.cs

│   │   ├── NetworkBootstrap.cs

│   │   ├── NetworkSpawnMediator.cs

│   │   ├── SubscriptionNetworkBehaviour.cs

│   │   ├── ISubscriptionService.cs

│   │   ├── IClientSubscriptionCallbacks.cs

│   │   └── IServerSubscriptionHandler.cs

│   ├── Server/

│   │   ├── ServerLifetimeScope.cs

│   │   ├── SubscriptionServiceServer.cs

│   │   └── MessageRouterServer.cs

│   ├── Client/

│   │   ├── ClientLifetimeScope.cs

│   │   ├── SubscriptionServiceClient.cs

│   │   └── MessageHandlerClient.cs

│   └── Messages/

│       └── NetworkMessages.cs

## Зависимости

- Unity 2022.3.62f3
- Mirror — сетевой транспорт
- VContainer — DI-контейнер
- UniTask — асинхронность без аллокаций

## Как работает

1. Клиент подключается к серверу
2. Для клиента спавнится SubscriptionNetworkBehaviour
3. Клиент вызывает Subscribe<HelloMessage>() — запрос уходит на сервер через Command
4. Сервер сохраняет подписку в словаре и шлет подтверждение через TargetRpc
5. Когда нужно разослать сообщение — сервер проходит по словарю и шлет только подписанным
6. Клиент получает сообщение, проверяет свою подписку и обрабатывает

Если NetworkBehaviour еще не готов при вызове Subscribe — подписка откладывается и автоматически отправляется, когда все компоненты будут на месте. Если подтверждение не приходит в течение 5 секунд — запрос повторяется.

## Запуск и проверка

Сцена MainScene уже собрана. Открыть ее и запускать оттуда.

### Вариант 1: Host (один редактор)

1. Открыть MainScene, нажать Play
2. NetworkBootstrap запустит Host автоматически (если Auto Start = true и Mode = Host)
3. В консоли появится лог о создании ServerScope и ClientScope
4. Нажать S — сервер отправит HelloMessage всем подписанным клиентам
5. В консоли клиента: Получено сообщение: Hello Client!

### Вариант 2: Server + Client через ParrelSync

ParrelSync позволяет запустить несколько копий редактора из одного проекта.

Установка:

Window → Package Manager → Add package from git URL
https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync

Проверка:

1. В главном редакторе (оригинал):
   - ParrelSync → Clones Manager → Add new clone (если клон еще не создан)
   - Open in new Editor для клона
   - В оригинале открыть MainScene, нажать Play — запустится Server (или Host, если так настроено)
   - В консоли: [GameLifetimeScope] Server: True, Client: False

2. В клоне:
   - Открыть MainScene, нажать Play — запустится Client
   - В консоли: [GameLifetimeScope] Server: False, Client: True
   - Автоматически подключится к серверу (если Auto Start = true и Mode = Client)
   - Клиент подпишется на HelloMessage — в консоли сервера: Клиент ... подписался на NetworkMessages.HelloMessage

3. В оригинале (сервер) нажать S:
   - В консоли сервера: NetworkMessages.HelloMessage отправлено 1 клиентам
   - В консоли клона (клиент):
     Получено сообщение: Hello Client!

4. Можно запустить несколько клонов — каждый получит сообщение только если подписан.

## Особенности

- Сервер и клиент живут в разных DI-контейнерах (ServerLifetimeScope / ClientLifetimeScope), создаются динамически в зависимости от режима
- SubscriptionNetworkBehaviour спавнится Mirror автоматически для каждого подключения и служит транспортом для команд подписки
- Все сетевые вызовы асинхронные через UniTask — нет блокировок главного потока
