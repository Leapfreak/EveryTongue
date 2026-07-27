# One-off: fix English short_name leftovers + the "Ruh" long_name typo in
# the local spavbl (Spanish) Bible. Standard Reina-Valera abbreviations.
import sqlite3
import sys

sys.stdout.reconfigure(encoding="utf-8")

SHORT = {
    60: "Jos", 70: "Jue", 80: "Rut",
    90: "1 S", 100: "2 S", 130: "1 Cr", 140: "2 Cr",
    250: "Ec", 260: "Cnt", 310: "Lm", 330: "Ez", 340: "Dn",
    470: "Mt", 480: "Mr", 490: "Lc", 500: "Jn", 510: "Hch",
    520: "Ro", 530: "1 Co", 540: "2 Co", 550: "Gá", 560: "Ef",
    570: "Fil", 590: "1 Ts", 600: "2 Ts", 610: "1 Ti", 620: "2 Ti",
    640: "Flm", 650: "He", 660: "Stg", 670: "1 P", 680: "2 P",
    730: "Ap",
}

db = sqlite3.connect(r"EveryTongue/bin/Publish/Bibles/spa/spavbl.sqlite3")
for bn, sn in SHORT.items():
    db.execute("UPDATE books SET short_name=? WHERE book_number=?", (sn, bn))
db.execute("UPDATE books SET long_name='Rut' WHERE book_number=80")
db.commit()
for r in db.execute("SELECT book_number, short_name, long_name FROM books ORDER BY book_number"):
    print(r)
db.close()
