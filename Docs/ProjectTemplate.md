# תבנית פרויקט — עיצוב תעשייתי

כל פרויקט הוא קובץ **Project** אחד בתיקייה `Assets/_Portfolio/Content/Projects/`.
במשחק הוא מופיע ככרטיס בפאנל של התחנה, ולחיצה עליו פותחת עמוד פרויקט במסך מלא:
תמונות משמאל (במחשב) או למעלה (בטלפון), טקסט בצד השני.

## איך מוסיפים פרויקט
1. ב־Unity, בחלון Project: `Content/Projects` → קליק ימני → **Create → Portfolio → Project**
   (או לשכפל את `Example_Project` עם Ctrl+D).
2. ממלאים את השדות ב־Inspector (הטבלה למטה).
3. פותחים את התחנה (למשל `Content/Stations/03_Projects`) → ברשימה **Projects** לוחצים + וגוררים את הקובץ.
   הסדר ברשימה = סדר הכרטיסים.
4. כש־Example_Project כבר לא צריך: להוציא אותו מהרשימה של התחנה (או למחוק).

## השדות

| קבוצה | שדה | מה כותבים |
|---|---|---|
| Card | **Title** | שם המוצר |
| | **Tagline** | משפט אחד: מה זה ולמי. "מנורת שולחן מתקפלת לחללים קטנים" |
| | **Year**, **Category** | 2026 · Lighting / Furniture / Consumer product… |
| | **Cover** | תמונת ה־Hero: המוצר הסופי, רקע נקי, תאורה חזקה. היא גם התמונה הקטנה בכרטיס |
| | **Accent** | צבע הפרויקט (כותרות, פס עליון, כפתורים) |
| Images | **Gallery** (2-5) | תמונה + כיתוב קצר. סדר מומלץ: הקשר/שימוש → סקיצות → מודלים ואב־טיפוס → פרט קרוב → המוצר בשימוש |
| | **Exploded View** + **Exploded Caption** | תמונת הפיצוץ. מוצגת אחרונה עם התווית EXPLODED VIEW |
| Story | **Challenge** | הבעיה / הבריף / התובנה. 2-3 משפטים |
| | **Solution** | מה עיצבת ולמה זה עובד |
| | **Process** | (לא חובה) מחקר, איטרציות, בדיקות — מה השתנה ולמה |
| Details | **Role**, **Duration**, **Context**, **Tools** | תפקיד, משך, הקשר (לקוח / קורס / תחרות / אישי), תוכנות ומכונות |
| | **Facts** | שורות חופשיות: Materials, Manufacturing, Dimensions, Weight… |
| | **Links** | Behance, וידאו, קבצי STL… (כתובת מלאה `https://`) |

שדה ריק פשוט לא מוצג.

## הכנת תמונות
- רוחב 1600–2048px מספיק. יותר מזה רק מגדיל את המשחק ומאט טעינה בטלפון.
- Import Settings של כל תמונה: **Texture Type = Sprite (2D and UI)**, **Max Size = 2048**, Compression: Normal → Apply.
- מומלץ לשמור את התמונות ב־`Assets/_Portfolio/Art/Projects/<שם הפרויקט>/`.
- Exploded view: רקע בהיר ואחיד, חלקים ממוספרים או עם קווי הפניה — הוא מוצג על רקע בהיר.

## מה ראינו אצל מעצבים אחרים (ומה התבנית לוקחת מזה)
- **Hero shot אחד חזק בפתיחה** — המוצר הסופי, נקי, "מושלם". מגדיר את הסיפור לפני הפרטים.
- **להראות תהליך, לא רק רנדרים** — סקיצות → קונספטים → מודלים → אב־טיפוס, כדי שיראו שהתוצאה לא מקרית.
- **Exploded view ורנדרים מפורטים** מוכיחים יכולת הנדסית ושליטה בייצור.
- **מעט טקסט, הרבה תמונה** — מגייסים סורקים, לא קוראים. כל קטע קצר וממוקד.
- **לארגן לפי פרויקט, לא לפי מיומנות** — כל פרויקט הוא סיפור שלם: בעיה → תהליך → פתרון.

מקורות: [Core77 — Hero shots, money shots and process pages](https://www.core77.com/hack2work/2009/09/hero_shots_money_shots_and_pro.asp),
[Yanko Design — Ten tips to improve your ID portfolio](https://www.yankodesign.com/2018/02/11/ten-tips-to-improve-your-industrial-design-portfolio/),
[Core77 boards — showing design process](https://boards.core77.com/t/how-to-show-design-process-in-portfolio-more-sketches/12450).
