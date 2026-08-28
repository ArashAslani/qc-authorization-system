# Authorization Architecture & Execution Specification — Qc

## 1. هدف سند

این سند معماری و مسیر اجرایی سیستم Authorization پروژه Qc را مشخص می‌کند.

هدف، ساخت یک هسته مدیریت دسترسی است که:

- در سیستم فعلی Qc قابل استفاده باشد.
- در آینده بتواند توسط چندین سیستم مستقل مصرف شود.
- Role-Based Access، Position-Based Access، Role Group، Individual Override و Delegation را پشتیبانی کند.
- دسترسی در سطح Module/Resource و Data Scope را مدیریت کند.
- قابلیت Propagation بر اساس ساختار سازمانی را داشته باشد.
- امکان اتصال به Workflowهای مختلف را فراهم کند.
- در آینده بتواند Constraintهای شرطی را پشتیبانی کند.
- قابل Audit و Debug باشد.
- از ایجاد یک Generic Rule Engine پیچیده و غیرقابل نگهداری جلوگیری کند.

اصل بنیادی معماری:

> **Grant = داده خام و بدون منطق**
>
> **Access Evaluation Engine = تنها مالک قوانین Authorization**

هیچ سرویس دیگری نباید مستقیماً تصمیم Allow/Deny بگیرد.

---

# 2. تصمیم معماری نهایی

معماری انتخاب‌شده، مدل:

**Grant + Access Evaluation Engine**

است.

Role، Position، RoleGroup، User، Delegation و سایر منابع، خودشان Authorization Engine مستقل ندارند.

آن‌ها فقط می‌توانند Grant ایجاد کنند یا Context لازم برای Evaluation را فراهم کنند.

ساختار مفهومی:

```text
                 Authorization Domain
                         │
          ┌──────────────┴──────────────┐
          │                             │
          ▼                             ▼
   Access Definition             Access Evaluation
          │                             │
   ┌──────┼──────┐              ┌───────┼────────┐
   ▼      ▼      ▼              ▼       ▼        ▼
Permission Role Position       Grants  Rules   Context
                                    │
                          ┌─────────┼─────────┐
                          ▼         ▼         ▼
                     Conflict  Propagation Delegation
                          │
                          ▼
                       Decision
                          │
                          ▼
                         Trace
```

---

# 3. اصول غیرقابل مذاکره

## 3.1 Grant باید Dumb Data باشد

Grant نباید خودش تصمیم بگیرد که معتبر است یا نه.

Grant فقط یک واقعیت Authorization را ذخیره می‌کند.

نمونه:

```text
Grant
--------------------------------
Id
Subject
Permission
Resource
Scope
Effect
SourceType
SourceId
ValidFrom
ValidTo
Priority
```

Grant نباید شامل منطق‌هایی مانند موارد زیر باشد:

```text
CanPropagate()
IsDelegationValid()
CanOverride()
EvaluateConstraint()
ResolveConflict()
```

تمام این تصمیم‌ها متعلق به Engine هستند.

---

# 4. Access Evaluation Engine

Engine تنها نقطه‌ای است که سؤال زیر را پاسخ می‌دهد:

```text
Can Subject X perform Action Y
on Resource Z
under Context C?
```

ورودی:

```text
AccessRequest
--------------------------------
Subject
Action
Resource
ResourceId
Context
```

خروجی:

```text
AccessDecision
--------------------------------
Effect
Reason
TraceId
```

که Effect می‌تواند:

```text
ALLOW
DENY
```

باشد.

---

# 5. Permission Model

Permission باید بر اساس سه مفهوم اصلی قابل تفکیک باشد:

```text
Resource
Action
Permission
```

مثال:

```text
Resource = Personnel
Action   = Update
Permission Code = PERSONNEL.UPDATE
```

نمونه Permissionها:

```text
PERSONNEL.READ
PERSONNEL.CREATE
PERSONNEL.UPDATE
PERSONNEL.DELETE

POSITION.READ
POSITION.CREATE
POSITION.UPDATE
POSITION.DELETE

POSITION.HIERARCHY.READ
POSITION.HIERARCHY.MANAGE
```

Permission باید Generic باشد تا سیستم‌های آینده بتوانند Resourceهای خودشان را معرفی کنند.

---

# 6. Permission Code و Identifier

برای Permissionها و Roleها می‌توان Business Code پایدار داشت.

مثال:

```text
Role
----------------
Id   = 100
Code = HR_MANAGER
```

```text
Permission
----------------
Id       = 1003
Resource = PERSONNEL
Action   = UPDATE
Code     = PERSONNEL.UPDATE
```

عددهای `100` و `1003` صرفاً Identifier هستند.

Core سیستم نباید منطق خود را بر Magic Numberها بنا کند.

منطق باید بر اساس مفاهیم پایدار مانند:

```text
PERSONNEL.UPDATE
HR_MANAGER
```

باشد.

---

# 7. Grant Model — Propagation Behavior by Subject Type

مدل پایه Grant:

```text
Grant
--------------------------------
Id
Subject
Permission
Resource
Scope
Effect
SourceType
SourceId
ValidFrom
ValidTo
Priority
```

`Subject` می‌تواند یکی از انواع زیر باشد:

```text
User
Position
Role
RoleGroup
```

اما **رفتار Propagation بر اساس Subject Type یکسان نیست**.

## 7.1 Position-sourced Grant

اگر Grant به یک `Position` تعلق داشته باشد:

```text
SubjectType = Position
```

Grant مشمول **Position Propagation Rules** است.

قانون Propagation برای Position به‌صورت نامتقارن تعریف می‌شود و برای عملیات Grant و Revoke جهت یکسانی ندارد.

## 7.2 User-sourced / Individual Grant

اگر Grant مستقیماً به یک User تعلق داشته باشد:

```text
SubjectType = User
SourceType = User
```

این Grant یک **Individual / Direct Grant** محسوب می‌شود و کاملاً ایزوله است.

```text
Individual Grant
        │
        └── No Position Propagation
```

Individual Grant:

- به Ancestorهای Position کاربر Propagate نمی‌شود.
- به Descendantهای Position کاربر Propagate نمی‌شود.
- با تغییر Position کاربر به‌صورت خودکار منتقل نمی‌شود.
- Revoke آن نیز Propagation سازمانی ایجاد نمی‌کند.

بنابراین:

> **Position-based Grant و Individual Grant دو مسیر مستقل Authorization هستند.**

این جداسازی برای جلوگیری از نشت ناخواسته‌ی دسترسی بین افراد و ساختار سازمانی الزامی است.

## 7.3 سایر Sourceها

Role، RoleGroup و Delegation در این بخش صرفاً به‌عنوان Grant Source شناخته می‌شوند.

رفتار Propagation آن‌ها باید توسط Rule مربوط به همان Source تعیین شود و نباید به‌صورت ضمنی از Position Propagation ارث ببرند.

---

# 8. Source Types

منابع اولیه Grant:

```text
Role
Position
RoleGroup
User
Delegation
```

در آینده ممکن است Sourceهای دیگری اضافه شوند.

اصل مهم:

> اضافه شدن Source جدید نباید Core Evaluation Engine را مجبور به بازنویسی کند.

Source باید فقط Grant تولید کند یا اطلاعات لازم برای تولید Grant را فراهم کند.

---

# 9. Priority Model

Conflict Resolution صرفاً بر اساس:

```text
Deny > Allow
```

نخواهد بود.

زیرا در Qc ممکن است Grantهای مختلف با منشأ متفاوت هم‌زمان وجود داشته باشند.

بنابراین Grantها باید از نظر Source/Precedence قابل اولویت‌بندی باشند.

اولویت V1:

```text
Individual Override
        >
Position Override
        >
Delegation
        >
Role / RoleGroup
        >
Propagated
```

Priority باید از همان V1 در مدل/Trace قابل مشاهده باشد.

نمونه:

```text
Grant A
Source = Role
Effect = ALLOW
Priority = 30

Grant B
Source = IndividualOverride
Effect = DENY
Priority = 100
```

نتیجه:

```text
DENY
```

در صورتی که دو Grant از یک سطح Priority باشند، Rule مربوط به Effect می‌تواند تعیین‌کننده باشد.

قاعده دقیق Conflict Resolution باید deterministic باشد.

---

# 10. Decision Trace

Decision Trace از V1 اجباری است.

هدف Trace این نیست که یک سیستم Logging پیچیده ساخته شود.

هدف این است که بتوان پاسخ داد:

> چرا این کاربر اجازه داشت یا نداشت؟

Trace حداقل باید بتواند مشخص کند:

```text
Subject
Requested Permission
Resource
ResourceId

Candidate Grants
Applicable Grants
Rejected Grants

SourceType
SourceId
Priority

Scope Result
Validity Result

Conflict Resolution

Final Decision
Reason
```

مثال:

```text
Decision: DENY

Subject:
User #80

Permission:
PERSONNEL.UPDATE

Candidate Grants:
Grant #120
Grant #121

Applicable:
Grant #121

Source:
Delegation #5001

Priority:
70

Scope:
Company #10

Requested Resource:
Personnel #900

Resource Company:
Company #20

Reason:
OUT_OF_SCOPE
```

Trace باید از ابتدا بخشی از طراحی Engine باشد.

---

# 11. V1 Evaluation Engine

V1 باید عمداً ساده باشد.

Pipeline اولیه:

```text
AccessRequest
      │
      ▼
Find Candidate Grants
      │
      ▼
Check Validity
      │
      ▼
Check Scope
      │
      ▼
Resolve Priority
      │
      ▼
Resolve Effect
      │
      ▼
Decision
      │
      ▼
Trace
```

V1 شامل موارد زیر نیست:

```text
Generic DSL
Generic Rule Engine
Complex Constraint Language
Automatic Materialized Propagation
Complex Policy Programming
```

---

# 12. Position Hierarchy

Position Hierarchy پیش‌نیاز Propagation است.

ساختار:

```text
Holding
   │
   ├── Company A
   │      │
   │      ├── Manager
   │      │     └── Supervisor
   │      │            └── Employee
   │      │
   │      └── ...
   │
   └── Company B
```

Hierarchy باید مستقل از Authorization Domain طراحی شود.

Authorization فقط از آن استفاده می‌کند.

---

# 13. Cycle Detection

Position Hierarchy نباید Cycle داشته باشد.

نمونه نامعتبر:

```text
A
 ↓
B
 ↓
C
 ↓
A
```

Cycle Detection باید در Domain Organization enforce شود.

Authorization Engine نباید مسئول صحت ساختار Organization باشد.

---

# 14. Position Propagation

Propagation در Qc یک عملیات عمومی و متقارن نیست.

**Grant Propagation و Revoke Propagation دو عملیات مستقل با جهت متفاوت هستند.**

نباید فرض شود که Revoke صرفاً معکوس Grant است.

## 14.1 Grant Propagation

هنگامی که یک Grant روی Position `P` ایجاد یا اعمال می‌شود:

```text
Grant Propagation:

Effective Positions =
    P + Ancestors(P)
```

یعنی Grant از Position موردنظر به سمت بالادست سازمانی Propagate می‌شود.

مثال:

```text
A
│
B
│
C
```

اگر Grant روی `C` اعمال شود:

```text
C → Grant
B → Effective Grant
A → Effective Grant
```

بنابراین:

```text
Grant(P)
    ↓
P + Ancestors(P)
```

### نکته

این Propagation باید **Computed** باشد و نباید Grantهای مشتق‌شده برای Ancestorها به‌عنوان Source of Truth ذخیره شوند.

## 14.2 Revoke Propagation

Revoke روی Position یک عملیات مستقل است.

هنگامی که یک Grant از Position `P` Revoke می‌شود:

```text
Revoke Propagation:

Effective Positions =
    P + Descendants(P)
```

یعنی Revoke از Position موردنظر به سمت پایین ساختار سازمانی Propagate می‌شود.

مثال:

```text
A
│
B
├── C
└── D
```

اگر Grant روی `B` Revoke شود:

```text
B → Revoke
C → Revoke
D → Revoke
```

اما:

```text
A → NOT affected
```

بنابراین:

```text
Revoke(P)
    ↓
P + Descendants(P)
```

و:

```text
Ancestors(P)
    ↓
NOT affected
```

## 14.3 Grant و Revoke نامتقارن هستند

این قانون باید به‌صورت صریح در Implementation رعایت شود:

```text
Grant:
    P + Ancestors(P)

Revoke:
    P + Descendants(P)
```

بنابراین نباید یک تابع Generic مانند:

```text
Propagate(Position, Operation)
```

با فرض جهت یکسان برای هر دو عملیات ایجاد شود.

منطق جهت‌یابی باید صراحتاً بین این دو عملیات تفکیک شود.

مثال مفهومی:

```text
Grant:
    ResolveAncestors(P)

Revoke:
    ResolveDescendants(P)
```

این تفاوت یک **Business Rule** است و نباید به‌عنوان Implementation Detail نادیده گرفته شود.

## 14.4 Propagation بدون Materialization

در هر دو عملیات، نتیجه Propagation نباید الزاماً به‌صورت Grantهای فیزیکی برای تمام Positionها ذخیره شود.

### Grant

```text
Position C
    │
    └── Grant X
          │
          ▼
      Evaluation
          │
          ▼
      Ancestors(C)
```

### Revoke

```text
Position B
    │
    └── Revoke X
          │
          ▼
      Evaluation
          │
          ▼
     Descendants(B)
```

تغییرات Position Hierarchy باید مستقیماً روی نتیجه Evaluation اثر بگذارند.

Cache یا Projection در آینده می‌تواند برای Performance اضافه شود، ولی نباید Source of Truth باشد.

## 14.5 Individual Grant در Propagation شرکت نمی‌کند

اگر:

```text
Grant.SubjectType = User
```

باشد، Position Hierarchy در Evaluation آن Grant هیچ نقش Propagationی ندارد.

مثال:

```text
Position:
    B
    │
    └── User: Ali

Individual Grant:
    Ali → PERSONNEL.UPDATE
```

این Grant:

```text
Ali
 ↓
PERSONNEL.UPDATE
```

باقی می‌ماند و به:

```text
B
Ancestors(B)
Descendants(B)
```

منتقل نمی‌شود.

حتی اگر Ali Position خود را تغییر دهد، این Individual Grant به Position جدید Propagate نمی‌شود.

---

# 15. Data Scope

Authorization فقط Module-Level نیست.

دو سطح اصلی:

```text
Module / Resource Permission
Data Scope
```

مثال:

```text
PERSONNEL.UPDATE
```

به‌تنهایی کافی نیست.

ممکن است:

```text
ALLOW
Scope = Company 10
```

باشد.

در نتیجه:

```text
Personnel #100
Company 10
→ ALLOW

Personnel #200
Company 20
→ DENY
```

---

# 16. CRUD و Sub-Resource

Permissionها باید بتوانند در سطح Action تفکیک شوند.

مثال:

```text
PERSONNEL.READ
PERSONNEL.CREATE
PERSONNEL.UPDATE
PERSONNEL.DELETE
```

و در صورت نیاز:

```text
PERSONNEL.SALARY.READ
PERSONNEL.CONTRACT.UPDATE
PERSONNEL.PERSONAL_INFO.READ
```

این قابلیت باید بدون تغییر Core Engine قابل توسعه باشد.

---

# 17. Revoke

Revoke صرفاً حذف یک رکورد Grant نیست.

Revoke باید به‌عنوان یک **Authorization Operation** مستقل در نظر گرفته شود.

## 17.1 Source Traceability

برای تشخیص اینکه چه دسترسی‌ای باید Revoke شود، منشأ Grant باید قابل ردیابی باشد:

```text
SourceType
SourceId
```

مثال:

```text
SourceType = Role
SourceId   = 100
```

یا:

```text
SourceType = Position
SourceId   = 205
```

یا:

```text
SourceType = Delegation
SourceId   = 5001
```

این اطلاعات برای:

- Revoke
- Audit
- Decision Trace
- Debugging
- Chain Revoke

ضروری هستند.

## 17.2 Position Revoke Propagation

Revoke روی یک Position یک عملیات مستقل با Rule Propagation مخصوص خودش است.

```text
Revoke on Position P
        ↓
P + Descendants(P)
```

و:

```text
Ancestors(P)
        ↓
NOT affected
```

بنابراین Revoke نباید صرفاً به‌عنوان حذف Grant از Position هدف پیاده‌سازی شود.

سیستم باید هنگام Evaluation یا اعمال Rule مربوطه، اثر Revoke را روی Position هدف و Descendantهای آن لحاظ کند.

## 17.3 Individual Revoke

اگر Grant از نوع Individual/User Grant باشد:

```text
SourceType = User
```

Revoke کاملاً ایزوله است.

```text
User Grant
     ↓
Revoke
     ↓
Only the individual grant is affected
```

هیچ Ancestor یا Descendant سازمانی نباید تحت تأثیر قرار گیرد.

## 17.4 Revoke و Priority

Revoke نباید قوانین Priority را دور بزند.

اگر چند مسیر Authorization وجود داشته باشد، Engine همچنان باید طبق مدل Priority و Conflict Resolution تصمیم بگیرد.

مثلاً:

```text
Role Grant
    ALLOW

Individual Override
    DENY
```

نتیجه بر اساس Priority:

```text
DENY
```

در حالی که Individual Grant همچنان از Propagation سازمانی مستقل است.

## 17.5 اصل نهایی Revoke

قاعده اجرایی:

```text
Position Grant:
    Grant  → P + Ancestors(P)
    Revoke → P + Descendants(P)

Individual Grant:
    Grant  → User only
    Revoke → User only
```

این چهار حالت باید به‌صورت مستقل در Test Suite پوشش داده شوند.

## 17.6 Mandatory Test Matrix

حداقل تست‌های Propagation:

```text
Position Grant
    P
    Ancestors(P)
    Descendants(P)

Position Revoke
    P
    Descendants(P)
    Ancestors(P) must remain unaffected

Individual Grant
    User only
    No Ancestor propagation
    No Descendant propagation

Individual Revoke
    User only
    No Ancestor propagation
    No Descendant propagation
```

مثال:

```text
A
│
B
│
C
```

### Grant روی C

```text
C = affected
B = affected
A = affected
```

### Revoke روی B

```text
B = affected
C = affected
A = NOT affected
```

### Individual Grant برای User روی C

```text
User = affected
C = NOT affected as Position
B = NOT affected
A = NOT affected
```

### Individual Revoke

```text
User = affected
C = NOT affected as Position
B = NOT affected
A = NOT affected
```

این رفتارها بخشی از Contract معماری Authorization هستند و نباید صرفاً به Implementation فعلی وابسته باشند.

---

# 18. Role و RoleGroup

Role و RoleGroup Grant Source هستند.

نمونه:

```text
HR_MANAGER
    ↓
PERSONNEL.READ
PERSONNEL.UPDATE
```

و:

```text
HR_ROLE_GROUP
    ↓
HR_MANAGER
HR_SPECIALIST
```

Role/RoleGroup نباید Authorization Engine مستقل ایجاد کنند.

---

# 19. Delegation

Delegation یکی از Sourceهای Grant است.

مثال:

```text
Ali
  │
  │ delegates
  ▼
Sara
```

نتیجه:

```text
Grant
--------------------------------
Subject = Sara
Permission = PERSONNEL.UPDATE
SourceType = Delegation
SourceId = 5001
ValidFrom = ...
ValidTo = ...
Scope = Company 10
```

Delegation Service نباید خودش تصمیم Allow/Deny بگیرد.

وظیفه Delegation:

```text
Create / Manage Delegation
        ↓
Produce Grant
        ↓
Evaluation Engine
```

---

# 20. Delegation Subset Enforcement

کاربر نباید بتواند بیشتر از دسترسی مؤثر خودش Delegation صادر کند.

مثال:

```text
Ali
Permissions:
    READ
    UPDATE
```

Ali نمی‌تواند:

```text
DELETE
```

را به Sara تفویض کند.

حتی اگر درخواست Delegation شامل آن باشد.

این قانون متعلق به Delegation/Evaluation Domain است و باید بر اساس Effective Access علی بررسی شود.

---

# 21. Chain Delegation

زنجیره Delegation:

```text
Ali
 ↓
Sara
 ↓
Reza
```

باید قابل کنترل باشد.

اگر Sara دسترسی را از Ali گرفته ولی اجازه Delegation آن را ندارد، نباید بتواند آن را به Reza منتقل کند.

بنابراین Delegation باید شامل مفهوم:

```text
Delegable
```

یا Rule معادل آن باشد.

اما این قابلیت در فاز Delegation اضافه می‌شود، نه V1.

---

# 22. Validity

Grant می‌تواند محدوده زمانی داشته باشد:

```text
ValidFrom
ValidTo
```

مثال:

```text
2026-09-01
2026-09-07
```

Engine هنگام Evaluation اعتبار زمانی را بررسی می‌کند.

Grant منقضی‌شده نباید نیازمند حذف شدن از دیتابیس باشد.

این اصل باعث می‌شود History و Audit حفظ شوند.

---

# 23. Constraint

Constraintها در V1 Generic نیستند.

نباید از ابتدا یک DSL مانند:

```text
IF Amount > 100000000
AND Company.Level = ...
AND User.Department = ...
```

بسازیم.

این کار فعلاً ممنوع است.

در صورت نیاز واقعی، Constraintها به‌صورت Typeهای شناخته‌شده اضافه می‌شوند.

مثال‌های احتمالی:

```text
AmountConstraint
TimeConstraint
ScopeConstraint
```

هر Constraint باید:

- قابل تست باشد.
- قابل Trace باشد.
- رفتار deterministic داشته باشد.
- بدون ایجاد DSL عمومی قابل نگهداری باشد.

---

# 24. Revoke

Revoke صرفاً حذف یک رکورد Grant نیست.

Revoke باید به‌عنوان یک **Authorization Operation** مستقل در نظر گرفته شود.

## 24.1 Source Traceability

برای تشخیص اینکه چه دسترسی‌ای باید Revoke شود، منشأ Grant باید قابل ردیابی باشد:

```text
SourceType
SourceId
```

مثال:

```text
SourceType = Role
SourceId   = 100
```

یا:

```text
SourceType = Position
SourceId   = 205
```

یا:

```text
SourceType = Delegation
SourceId   = 5001
```

این اطلاعات برای:

- Revoke
- Audit
- Decision Trace
- Debugging
- Chain Revoke

ضروری هستند.

## 24.2 Position Revoke Propagation

Revoke روی یک Position یک عملیات مستقل با Rule Propagation مخصوص خودش است.

```text
Revoke on Position P
        ↓
P + Descendants(P)
```

و:

```text
Ancestors(P)
        ↓
NOT affected
```

بنابراین Revoke نباید صرفاً به‌عنوان حذف Grant از Position هدف پیاده‌سازی شود.

سیستم باید هنگام Evaluation یا اعمال Rule مربوطه، اثر Revoke را روی Position هدف و Descendantهای آن لحاظ کند.

## 24.3 Individual Revoke

اگر Grant از نوع Individual/User Grant باشد:

```text
SourceType = User
```

Revoke کاملاً ایزوله است.

```text
User Grant
     ↓
Revoke
     ↓
Only the individual grant is affected
```

هیچ Ancestor یا Descendant سازمانی نباید تحت تأثیر قرار گیرد.

## 24.4 Revoke و Priority

Revoke نباید قوانین Priority را دور بزند.

اگر چند مسیر Authorization وجود داشته باشد، Engine همچنان باید طبق مدل Priority و Conflict Resolution تصمیم بگیرد.

مثلاً:

```text
Role Grant
    ALLOW

Individual Override
    DENY
```

نتیجه بر اساس Priority:

```text
DENY
```

در حالی که Individual Grant همچنان از Propagation سازمانی مستقل است.

## 24.5 اصل نهایی Revoke

قاعده اجرایی:

```text
Position Grant:
    Grant  → P + Ancestors(P)
    Revoke → P + Descendants(P)

Individual Grant:
    Grant  → User only
    Revoke → User only
```

این چهار حالت باید به‌صورت مستقل در Test Suite پوشش داده شوند.

## 24.6 Mandatory Test Matrix

حداقل تست‌های Propagation:

```text
Position Grant
    P
    Ancestors(P)
    Descendants(P)

Position Revoke
    P
    Descendants(P)
    Ancestors(P) must remain unaffected

Individual Grant
    User only
    No Ancestor propagation
    No Descendant propagation

Individual Revoke
    User only
    No Ancestor propagation
    No Descendant propagation
```

مثال:

```text
A
│
B
│
C
```

### Grant روی C

```text
C = affected
B = affected
A = affected
```

### Revoke روی B

```text
B = affected
C = affected
A = NOT affected
```

### Individual Grant برای User روی C

```text
User = affected
C = NOT affected as Position
B = NOT affected
A = NOT affected
```

### Individual Revoke

```text
User = affected
C = NOT affected as Position
B = NOT affected
A = NOT affected
```

این رفتارها بخشی از Contract معماری Authorization هستند و نباید صرفاً به Implementation فعلی وابسته باشند.

---

# 25. Workflow Integration

Workflow نباید Authorization Engine مستقل داشته باشد.

Workflow فقط Requirement را اعلام می‌کند.

مثال:

```text
Workflow:
Purchase Approval

Step:
Finance Approval

Required Permission:
PURCHASE.FINANCE_APPROVE
```

Workflow سپس درخواست Evaluation می‌دهد:

```text
AccessRequest
--------------------------------
Subject = User #80
Action = FINANCE_APPROVE
Resource = Purchase
ResourceId = 152
Context = ...
```

و Engine پاسخ می‌دهد:

```text
ALLOW / DENY
```

بنابراین:

```text
Workflow
    ↓
Authorization Request
    ↓
Access Evaluation Engine
    ↓
Decision
```

این جداسازی باعث می‌شود Workflowهای مختلف بتوانند از یک Authorization Core استفاده کنند.

---

# 26. Audit

Audit با Decision Trace متفاوت است.

### Decision Trace

پاسخ می‌دهد:

> چرا این Evaluation به ALLOW/DENY رسید؟

### Audit

پاسخ می‌دهد:

> چه تغییری در Authorization System اتفاق افتاد؟

مثلاً:

```text
Role Created
Permission Added
Grant Created
Grant Revoked
Delegation Created
Position Permission Changed
```

این دو مفهوم باید از هم جدا بمانند.

---

# 27. ترتیب اجرای فازها

## Phase 01 — Organization Foundation

هدف:

ساخت کامل Foundation سازمانی.

```text
Personnel
Position
PositionAssignment
Position Hierarchy
Cycle Detection
```

وضعیت فعلی:

```text
Personnel          ✅
Position           ✅
PositionAssignment ✅
Position Hierarchy ⏳
Cycle Detection    ⏳
```

این Phase باید تکمیل شود.

---

## Phase 02 — Access Definition & Grant

پیاده‌سازی:

```text
Permission
Resource
Action
Role
RolePermission
Grant
Grant Source
Scope
Effect
Validity
Priority
```

در این Phase هنوز:

```text
Propagation ❌
Complex Delegation ❌
Generic Constraints ❌
```

وجود ندارند.

---

## Phase 03 — Minimal Access Evaluation

ساخت:

```text
IAccessEvaluator
AccessRequest
AccessDecision
Candidate Grant Resolver
Priority Resolver
Conflict Resolver
Decision Trace
```

قوانین:

```text
Grant Resolution
+
Validity
+
Scope
+
Priority
+
Allow/Deny
```

هدف:

یک Engine کوچک، deterministic و کاملاً تست‌شده.

---

## Phase 04 — Propagation

اضافه شدن:

```text
Position Ancestors
Propagation Rules
Propagation Evaluation
```

بدون Materialization.

Hierarchy از Organization Domain خوانده می‌شود.

---

## Phase 05 — Delegation

اضافه شدن:

```text
Delegation
Validity
Subset Enforcement
Delegation Source
Delegation Chain
Delegation Rules
```

Delegation فقط Grant تولید می‌کند.

---

## Phase 06 — Constraints

فقط بر اساس نیاز واقعی سیستم.

شروع با Constraintهای محدود و شناخته‌شده:

```text
Amount
Time
Scope
```

بدون DSL عمومی.

---

## Phase 07 — Workflow Integration

اتصال Workflowها به:

```text
AccessRequest
AccessEvaluation
Decision
```

Workflow صاحب Authorization Logic نخواهد شد.

---

## Phase 08 — Performance & Scale

فقط پس از وجود Use Case واقعی:

```text
Caching
Projection
Materialized Read Models
Batch Evaluation
Distributed Cache
```

Performance Optimization نباید مدل Domain را آلوده کند.

---

# 28. تست‌های اجباری

هر مرحله باید با Test پوشش داده شود.

حداقل سناریوهای V1:

```text
Role → Allow
Role → Deny

Position → Allow
Position → Deny

User Direct Grant → Allow
User Direct Deny → Deny

Allow + Deny → Priority Resolution

Valid Grant → Allow
Expired Grant → Deny

In-Scope → Allow
Out-of-Scope → Deny

Multiple Grants → Deterministic Result

Decision → Correct Trace
```

بعد از اضافه شدن Propagation:

```text
Position Grant
Ancestor Evaluation
Hierarchy Change
Position Move
Cycle Prevention
```

بعد از Delegation:

```text
Valid Delegation
Expired Delegation
Subset Violation
Delegation Chain
Revoke
```

بعد از Constraint:

```text
Amount <= Limit
Amount > Limit
Time Valid
Time Invalid
```

همچنین باید تست‌های اجباری نامتقارن Propagation از بخش 17.6 اجرا شوند.

---

# 29. معیارهای پذیرش معماری

Architecture زمانی موفق محسوب می‌شود که:

### اصل 1

هیچ سرویس دیگری به‌جز Authorization Engine تصمیم Allow/Deny نگیرد.

### اصل 2

Grant فاقد Business Logic باشد.

### اصل 3

Propagation به‌صورت Computed باشد.

### اصل 4

Source هر Grant قابل Trace باشد.

### اصل 5

Conflict Resolution deterministic باشد.

### اصل 6

Decision قابل توضیح باشد.

### اصل 7

Workflow بدون پیاده‌سازی Authorization Logic مستقل بتواند از Engine استفاده کند.

### اصل 8

Constraintها بدون ساخت DSL عمومی قابل توسعه باشند.

### اصل 9

افزودن Source جدید حداقل تغییر را در Core ایجاد کند.

### اصل 10

Organization Domain و Authorization Domain به‌صورت مستقل ولی قابل اتصال باقی بمانند.

### اصل 11

Grant Propagation و Revoke Propagation به‌صورت مستقل و نامتقارن پیاده‌سازی شوند.

### اصل 12

Individual/User Grants کاملاً از Position Propagation جدا باشند.

---

# 30. چیزهایی که عمداً فعلاً نمی‌سازیم

برای جلوگیری از Over-Engineering:

```text
Generic Rule Engine          ❌
Authorization DSL            ❌
Generic Policy Language      ❌
Materialized Propagation     ❌
Distributed Authorization    ❌
Complex Constraint Engine   ❌
Universal Expression Parser ❌
```

این‌ها فقط زمانی اضافه می‌شوند که Use Case واقعی وجود داشته باشد.

---

# 31. Architectural North Star

معماری نهایی باید بتواند در ساده‌ترین حالت:

```text
User
 ↓
Role
 ↓
Permission
 ↓
Grant
 ↓
Evaluation
 ↓
ALLOW
```

را با کمترین پیچیدگی اجرا کند.

اما همان Core باید بتواند در آینده به:

```text
User
 ↓
Position
 ↓
RoleGroup
 ↓
Delegation
 ↓
Propagation
 ↓
Data Scope
 ↓
Constraint
 ↓
Workflow Context
 ↓
Conflict Resolution
 ↓
Decision
 ↓
Decision Trace
```

گسترش پیدا کند.

بدون اینکه مدل پایه Grant یا قرارداد اصلی Access Evaluation شکسته شود.

---

# 32. اصل نهایی معماری

سیستم نباید از ابتدا همه قابلیت‌های Enterprise را پیاده‌سازی کند.

بلکه باید:

> **Enterprise-ready باشد، بدون اینکه از روز اول Enterprise-complex باشد.**

بنابراین:

```text
Simple Core
     +
Stable Grant Model
     +
Single Evaluation Point
     +
Deterministic Rules
     +
Traceability
     ↓
Incremental Enterprise Capabilities
```

این ساختار مبنای رسمی اجرای Access Management در Qc است.
