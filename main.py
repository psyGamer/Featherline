# tntfalle, 16.11.2021
# Kataiser, cake, TheRoboMan

import random
import yaml
from pyeasyga import pyeasyga
from feather_sim import sim


def main():
    s = load_settings()
    s = format_settings(s)
    if s["spinner_file"] != "":
        check_file(s["spinner_file"])
        s["spinners"] += import_spinners(s["spinner_file"])

    ga = pyeasyga.GeneticAlgorithm(s, s["population"], s["generations"], s["mutation_probability"],
                                   s["crossover_probability"], s["elitism"])
    ga.fitness_function = fitness
    ga.create_individual = create_individual
    ga.crossover_function = crossover
    ga.mutate_function = mutate
    print("Starting Genetic Algorithm\n\n")
    ga.run()

    print("Last generation:\n")
    for individual in ga.last_generation():
        print(individual)

    print("\n\nBest individual:\n")
    print(ga.best_individual())

    print(to_inputs(ga.best_individual()[1]))


# change this to get better results (cateline)
def fitness(individual: list[float], s: dict[str, any]) -> float:
    posx, posy, speedx, speedy, dead = sim(s["pos_x"], s["pos_y"], individual, s["spinners"], s["killboxes"],
                                           s["boost_x"], s["boost_y"])
    if s["goal"] == "x":
        return (posx if s["prim_dir"] else -posx) - (100000 if dead else 0) - (
            0 if (s["sec_min"] <= posy <= s["sec_max"]) or (s["sec_max"] <= posy <= s["sec_min"])
            else s["sec_factor"] * min(abs(posy - s["sec_min"]), abs(posy - s["sec_max"])))
    elif s["goal"] == "y":
        return (posy if s["prim_dir"] else -posy) - (100000 if dead else 0) - (
            0 if (s["sec_min"] <= posx <= s["sec_max"]) or (s["sec_max"] <= posx <= s["sec_min"])
            else s["sec_factor"] * min(abs(posx - s["sec_min"]), abs(posx - s["sec_max"])))
    else:
        return 100000 - (s["goal_x"] - posx) ** 2 + (s["goal_y"] - posy) ** 2 - (100000 if dead else 0)


def crossover(parent_1: list[float], parent_2: list[float]) -> tuple[any, any]:
    if random.random() < 0.5:
        # exchange parts
        index = random.randrange(1, len(parent_1))
        child_1 = parent_1[:index] + parent_2[index:]
        child_2 = parent_2[:index] + parent_1[index:]
    else:
        # exchange every other value
        child_1, child_2 = parent_1, parent_2
        for i in range(0, len(parent_1), 2):
            child_1[i], child_2[i] = child_2[i], child_1[i]
    return child_1, child_2


def mutate(individual: list[float]):
    # change multiple values
    length = random.randrange(int(len(individual) / 2))
    x = random.randrange(len(individual) - length)
    increment = random.randint(-4000, 4000) / 1000
    for i in range(x, x + length):
        individual[i] = round(individual[i] + increment, 3)
        if individual[i] >= 360:
            individual[i] -= 360
        elif individual[i] < 0:
            individual[i] += 360

    # change a single value (for more precise optimisation) (rounding missing)
    # individual[random.randrange(len(individual))] += random.randint(-4000, 4000) / 1000

    # simplify (bias towards shorter inputs)
    # length = random.randrange(int(len(individual) / 4))
    # start = random.randint(1, len(individual) - length)
    # for i in range(start, start + length):
    #     individual[i] = round(individual[start - 1], 3)


def create_individual(s: dict[str, any]) -> list[float]:
    return s["favorite"] if s["favorite"] else [random.randrange(0, 360000) / 1000 for _ in range(s["dna_length"])]


def format_settings(s: dict[str, any]) -> dict[str, any]:
    s["spinner_file"] = s["spinner_file"].strip("\n").strip()
    s["spinners"] = [[float(j) for j in i.split(",")] for i in s["spinners"].split()]
    s["killboxes"] = [[float(j) for j in i.split(",")] for i in s["killboxes"].split()]

    if s["favorite"].strip("\n").strip() == "":
        s["favorite"] = []
    else:
        s["favorite"] = s["favorite"].split()
        n_fav = []

        for i in s["favorite"]:
            cache = i.split(",")

            try:
                for j in range(int(cache[0])):
                    n_fav.append(float(cache[2]))
            except ValueError:
                raise SystemError(f"Invalid favorite:\n{i}")

        if len(n_fav) != s["dna_length"]:
            raise SystemError(f"Invalid favorite:\nLength {len(n_fav)} isn't equal to dna_length")

        s["favorite"] = n_fav

    if s["goal"] != "x" and s["goal"] != "y" and s["goal"] != "radial":
        raise SystemError("Invalid goal: must be x, y or radial")

    print("Config OK")
    return s


def load_settings() -> dict[str, any]:
    try:
        with open("config.yaml", "r") as config_file:
            settings = yaml.safe_load(config_file)

        # check for correct keys/values
        settings["killboxes"] = str(settings["killboxes"])
        settings["spinners"] = str(settings["spinners"])
        settings["favorite"] = str(settings["favorite"])
        settings["goal"] = str(settings["goal"])
        settings["spinner_file"] = str(settings["spinner_file"])
        settings["dna_length"] = int(settings["dna_length"])
        settings["population"] = int(settings["population"])
        settings["generations"] = int(settings["generations"])
        settings["prim_dir"] = bool(int(settings["prim_dir"]))
        settings["elitism"] = bool(int(settings["elitism"]))
        settings["pos_x"] = float(settings["pos_x"])
        settings["pos_y"] = float(settings["pos_y"])
        settings["boost_x"] = float(settings["boost_x"])
        settings["boost_y"] = float(settings["boost_y"])
        settings["crossover_probability"] = float(settings["crossover_probability"])
        settings["mutation_probability"] = float(settings["mutation_probability"])
        settings["sec_min"] = float(settings["sec_min"])
        settings["sec_max"] = float(settings["sec_max"])
        settings["sec_factor"] = float(settings["sec_factor"])
        settings["goal_x"] = float(settings["goal_x"])
        settings["goal_y"] = float(settings["goal_y"])

        return settings
    except yaml.YAMLError as error:
        print(f"Couldn't parse settings:\n{str(error)}")
    except (ValueError, TypeError, KeyError, FileNotFoundError) as error:
        print(f"Invalid config file: {repr(error)}")

    raise SystemExit


def check_file(path: str):
    try:
        with open(path, "r") as file:
            file.read()

        print("Spinner file OK", path)
        return
    except FileNotFoundError:
        print("Spinner file error: file not found")
    except PermissionError:
        print("Spinner file error: no reading permission")
    raise SystemError


def import_spinners(path: str) -> list[(int, int)]:
    from re import findall
    with open(path, "r") as file:
        gameinfo = file.read()
    matches = findall(r"CrystalStaticSpinner: (-?\d+\.\d+), (-?\d+\.\d+)", gameinfo)
    return [(round(float(m[0])), round(float(m[1]))) for m in matches]


def to_inputs(s: tuple[float, list[float]]) -> str:
    r = ""
    for i in s:
        r += "\n1,F," + str(i)
    return r


if __name__ == "__main__":
    main()
