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

   exam_and_grading       1.00      1.00      1.00         8
course_registration       1.00      1.00      1.00         8
    digital_systems       1.00      1.00      1.00         8
   student_services       1.00      1.00      1.00         8
            library       1.00      1.00      1.00         8
       student_life       1.00      1.00      1.00        12
scholarship_support       1.00      1.00      1.00         8

           accuracy                           1.00        60
          macro avg       1.00      1.00      1.00        60
       weighted avg       1.00      1.00      1.00        60
```

## Confusion Matrix
```text
labels = exam_and_grading, course_registration, digital_systems, student_services, library, student_life, scholarship_support
exam_and_grading: [8, 0, 0, 0, 0, 0, 0]
course_registration: [0, 8, 0, 0, 0, 0, 0]
digital_systems: [0, 0, 8, 0, 0, 0, 0]
student_services: [0, 0, 0, 8, 0, 0, 0]
library: [0, 0, 0, 0, 8, 0, 0]
student_life: [0, 0, 0, 0, 0, 12, 0]
scholarship_support: [0, 0, 0, 0, 0, 0, 8]
```

## Mismatches
- Hata yok
