# EduAI Phase C Hard Evaluation

- Test seti: `60` zor örnek
- KB hit rate: `0.9667`

## Per-Intent KB Hit
- `exam_and_grading`: `7/8` (`0.8750`)
- `course_registration`: `8/8` (`1.0000`)
- `digital_systems`: `8/8` (`1.0000`)
- `student_services`: `8/8` (`1.0000`)
- `library`: `7/8` (`0.8750`)
- `student_life`: `12/12` (`1.0000`)
- `scholarship_support`: `8/8` (`1.0000`)

## Classification Report
```text
                     precision    recall  f1-score   support

   exam_and_grading       0.80      1.00      0.89         8
course_registration       1.00      0.88      0.93         8
    digital_systems       0.89      1.00      0.94         8
   student_services       1.00      1.00      1.00         8
            library       1.00      0.75      0.86         8
       student_life       0.86      1.00      0.92        12
scholarship_support       1.00      0.75      0.86         8

           accuracy                           0.92        60
          macro avg       0.94      0.91      0.91        60
       weighted avg       0.93      0.92      0.91        60
```

## Confusion Matrix
```text
labels = exam_and_grading, course_registration, digital_systems, student_services, library, student_life, scholarship_support
exam_and_grading: [8, 0, 0, 0, 0, 0, 0]
course_registration: [1, 7, 0, 0, 0, 0, 0]
digital_systems: [0, 0, 8, 0, 0, 0, 0]
student_services: [0, 0, 0, 8, 0, 0, 0]
library: [0, 0, 1, 0, 6, 1, 0]
student_life: [0, 0, 0, 0, 0, 12, 0]
scholarship_support: [1, 0, 0, 0, 0, 1, 6]
```

## Mismatches
- `gano 1.50 altina dusunce ustten ders alamiyor muyum` | beklenen=`course_registration` tahmin=`exam_and_grading` guven=`0.82` kb=`True` topic=`mezuniyet_gano'
- `bireysel calisma odasi rezervasyonlu mu` | beklenen=`library` tahmin=`student_life` guven=`0.79` kb=`True` topic=`ucretsiz_yemek_2024'
- `kutuphane hesabi sifresi varsayilan 1 mi` | beklenen=`library` tahmin=`digital_systems` guven=`0.8` kb=`True` topic=`varsayilan_sifre'
- `tesvik bursu gano 2.50 altina dusunce kesilir mi` | beklenen=`scholarship_support` tahmin=`exam_and_grading` guven=`0.82` kb=`True` topic=`mezuniyet_gano'
- `barinma yardimi resmi belgelerde geciyor mu` | beklenen=`scholarship_support` tahmin=`student_life` guven=`0.4379` kb=`True` topic=`barinma_genel_yonlendirme'
