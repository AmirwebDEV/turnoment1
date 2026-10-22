usage of database.py:

Database.load(
    task = {name of task},
    flag = {data category or loading method},
    sub_cat = {specify sub category}
    )
returns a data 

Database.save(
    task = {name of task},
    flag = {data category or loading method},
    sub_cat = {specify sub category},
    data_to_save = {the data you wanted to save}
    )
overwrites the save file

Database.save(
    task = {name of task},
    flag = {data category or loading method},
    sub_cat = {specify sub category},
    data_to_save = {the data you wanted to save}
    )
adds data into existing ones